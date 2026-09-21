using Microsoft.AspNetCore.SignalR;
using Xml.Api.Features.ScheduleUpload.Infrastructure;

namespace Xml.Api.Features.ScheduleUpload
{
    /// <summary>
    /// Фасад(Оркестратор) процесса импорта расписания
    /// Внедряет и координирует работу всех наших модулей реактивно в SignalR
    /// </summary>
    public class XmlScheduleParserService
    {
        // Внедрение через конструктор
        private readonly ScheduleReader _reader;
        private readonly ScheduleMapper _mapper;
        private readonly ScheduleSaver _saver;
        private readonly IHubContext<ScheduleHub> _hubContext;
        private readonly UploadProgressTracker _progressTracker;

        // Регистрация модулей
        public XmlScheduleParserService(
            ScheduleReader reader,
            ScheduleMapper mapper,
            ScheduleSaver saver,
            IHubContext<ScheduleHub> hubContext,
            UploadProgressTracker progressTracker)
        {
            _reader = reader;
            _mapper = mapper;
            _saver = saver;
            _hubContext = hubContext;
            _progressTracker = progressTracker;
        }

        /// <summary>
        /// Запускает полный цикл импорта XML файла в базу данных.
        /// </summary>
        /// <param name="filePath">Путь к сохранённому XML файлу</param>
        /// <param name="jobId">Уникальный идентификатор фоновой задачи.</param>
        /// <param name="connectionId">Уникальный ID веб-сокет соединения пользователя (null, если пользователь закрыл вкладку).</param>
        /// <param name="cancellationToken">Токен принудительной отмены операции.</param>
        public async Task<bool> ParseAndSaveAsync(string filePath, string jobId, CancellationToken cancellationToken)
        {
            async Task ReportProgressAsync(int percent1, string status1)
            {
                _progressTracker.UpdateProgress(jobId, status1, percent1);

                await _hubContext.Clients.Group(jobId).SendAsync("ReceiveProgress", percent1, status1);
            }

            try
            {
                // Потоковое чтение файла
                var dtos = await _reader.ReadAsync(filePath, async (percent1, status1) =>
                {
                    await ReportProgressAsync(percent1, status1);
                }, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                await ReportProgressAsync(92, "Индексация справочников и подготовка связей..");
                var (chairs, discs, groups, preps, buildings, rooms, rawDtos) = _mapper.Map(dtos);

                cancellationToken.ThrowIfCancellationRequested();

                // Транзакционное сохранение
                await ReportProgressAsync(96, "Очистка таблиц СУБД и пакетное сохранение расписания в PostgreSQL...");
                await _saver.SaveAsync(chairs, discs, groups, preps, buildings, rooms, rawDtos, cancellationToken);

                //Финальный этап
                _progressTracker.UpdateProgress(jobId, "Импорт успешно завершен!", 100, isCompleted: true);
                await _hubContext.Clients.Group(jobId).SendAsync("ReceiveResult", true, "Расписание успешно импортировано в базу данных!");

                return true;
            }
            catch (OperationCanceledException)// Проверка на cancellation, чтобы отмена не вызывала ошибку
            {
                _progressTracker.UpdateProgress(jobId, "Операция отменена пользователем", 0, isCompleted: true, errorMessage: "Отмена");
                await _hubContext.Clients.Group(jobId).SendAsync("ReceiveResult", false, "Операция импорта была отменена пользователем.");

                // Удаляем временный файл с диска, если он остался
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                return false;
            }
            catch (Exception ex)//Проверка на сторонние ошибки
            {
                _progressTracker.UpdateProgress(jobId, "Критическая ошибка импорта", 0, isCompleted: true, errorMessage: ex.Message);
                await _hubContext.Clients.Group(jobId).SendAsync("ReceiveResult", false, $"Критическая ошибка при обработке: {ex.Message}");

                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                return false;
            }
        }
    }

}
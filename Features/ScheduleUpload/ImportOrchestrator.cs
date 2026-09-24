using Microsoft.AspNetCore.SignalR;
using Xml.Api.Features.ScheduleUpload.Infrastructure;

namespace Xml.Api.Features.ScheduleUpload
{
    /// <summary>
    /// Фасад(Оркестратор) процесса импорта расписания
    /// Внедряет и координирует работу всех наших модулей реактивно в SignalR
    /// </summary>
    public class ImportOrchestrator(
        Reader reader,
        Mapper mapper,
        Saver saver,
        IHubContext<ScheduleHub> hubContext,
        UploadProgressTracker progressTracker)
    {
        // Внедрение через конструктор
        private readonly Reader _reader = reader;
        private readonly Mapper _mapper = mapper;
        private readonly Saver _saver = saver;
        private readonly IHubContext<ScheduleHub> _hubContext = hubContext;
        private readonly UploadProgressTracker _progressTracker = progressTracker;

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

                await _hubContext.Clients.Group(jobId).SendAsync("ReceiveProgress", percent1, status1, cancellationToken: cancellationToken);
            }

            try
            {
                // Потоковое чтение файла
                var dtos = await _reader.ReadAsync(filePath, async (percent1, status1) =>
                {
                    await ReportProgressAsync(percent1, status1);
                }, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                await ReportProgressAsync(72, "Выделение уникальных кафедр и предметов...");
                await Task.Delay(100, cancellationToken); // Микро-пауза для сглаживания

                await ReportProgressAsync(76, "Индексация учебных групп и преподавателей...");
                var (chairs, discs, groups, preps, buildings, rooms, rawDtos) = _mapper.Map(dtos);

                await ReportProgressAsync(80, "Структурирование корпусов и аудиторий в памяти...");
                await Task.Delay(100, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                await _saver.SaveAsync(chairs, discs, groups, preps, buildings, rooms, rawDtos, async (percent, status) =>
                {
                    await ReportProgressAsync(percent, status);
                }, cancellationToken);

                //Финальный этап
                _progressTracker.UpdateProgress(jobId, "Импорт успешно завершен!", 100, isCompleted: true);
                await _hubContext.Clients.Group(jobId).SendAsync("ReceiveResult", true, "Расписание успешно импортировано в базу данных!", cancellationToken: cancellationToken);

                return true;
            }
            catch (OperationCanceledException)// Проверка на cancellation, чтобы отмена не вызывала ошибку
            {
                _progressTracker.UpdateProgress(jobId, "Операция отменена пользователем", 0, isCompleted: true, errorMessage: "Отмена");
                await _hubContext.Clients.Group(jobId).SendAsync("ReceiveResult", false, "Операция импорта была отменена пользователем.", cancellationToken: cancellationToken);

                // Удаляем временный файл с диска, если он остался
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                return false;
            }
            catch (Exception ex)//Проверка на сторонние ошибки
            {
                _progressTracker.UpdateProgress(jobId, "Критическая ошибка импорта", 0, isCompleted: true, errorMessage: ex.Message);
                await _hubContext.Clients.Group(jobId).SendAsync("ReceiveResult", false, $"Критическая ошибка при обработке: {ex.Message}", cancellationToken: cancellationToken);

                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                return false;
            }
        }
    }

}
using System.Diagnostics;
using System.Xml;

namespace Xml.Api.Features.ScheduleUpload
{
    /// <summary>
    /// Модуль потокового чтения XML-файла расписания.
    /// Отвечает за парсинг сырого XML в плоские структуры DTO.
    /// </summary>
    public class ScheduleReader
    {

        //Const#

        /// <summary>
        /// Максимальный процент прогресса, который может выставить модуль чтения.
        /// Оставшийся 10% зарезервированы за модулем сохранения в БД (Commit транзакции).
        /// </summary>
        private const int MaxReaderProgressPercent = 90;

        /// <summary>
        /// Интервал времени в миллисекундах для отправки отчетов о прогрессе на фронтенд.
        /// </summary>
        private const long ProgressReportIntervalMs = 500;

        /// <summary>
        /// Размер буфера для асинхронного чтения файлового потока (64 КБ).
        /// Оптимально для современных серверных дисковых подсистем.
        /// </summary>
        private const int FileStreamBufferSize = 64 * 1024;

        //Methods#

        /// <summary>
        /// Асинхронно читает XML-файл и извлекает из него список объектов расписания.
        /// Использует XmlReader для минимизации потребления оперативной памяти (стриминг).
        /// </summary>
        /// <param name="filePath">Абсолютный путь к XML-файлу на диске сервера</param>
        /// <param name="onProgress">Делегат для передачи текущего процента обработки</param>
        /// <param name="cancellationToken">Токен для отмены операции, например пользователем</param>
        /// <returns>Коллекция плоских DTO с данными из файла</returns>
        public async Task<IReadOnlyList<SubjectDto>> ReadAsync(
            string filePath, 
            Func<int, string, Task> onProgress,
            CancellationToken cancellationToken)
        {
            //Проверка на отмену перед входом в парсинг
            cancellationToken.ThrowIfCancellationRequested();

            var subjects = new List<SubjectDto>();

            // Мера безопасности, запрещаем обработку внешних сущностей DTD (защита XML External Entity Attack)
            var settings = new XmlReaderSettings
            {
                Async = true,
                DtdProcessing = DtdProcessing.Prohibit
            };

            // Открываем файловый поток в асинхронном режиме с заданным размером буфера 4096
            using var stream = new FileStream(
                filePath, 
                FileMode.Open, 
                FileAccess.Read, 
                FileShare.Read, 
                FileStreamBufferSize, 
                useAsync: true);

            using var reader = XmlReader.Create(stream, settings);

            long fileLength = stream.Length;
            int lastReportedPercent = -1; //Значение позволяет отправить событие, даже если значение равно 0 

            //Для отсчёта времени между интервалами
            var stopwatch = Stopwatch.StartNew();

            while (await reader.ReadAsync())
            {
                // XmlReader.ReadAsync не поддерживает CancellationToken,
                // поэтому проверяем отмену вручную на каждой итерации.
                cancellationToken.ThrowIfCancellationRequested();

                // Реагируем только на файлы XML с элементами "subject" внутри
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "subject")
                {
                    var dto = new SubjectDto
                    {
                        //Проверка аттрибута через оператор Элвиса. При значении null возвращает правое значение.
                        IdSubg = reader.GetAttribute("IDSubg") ?? "",
                        DiscName = reader.GetAttribute("disc") ?? "Не указано",
                        Type = reader.GetAttribute("type") ?? "Л",
                        ChairName = reader.GetAttribute("chair") ?? "Не указана",
                        PrepId = reader.GetAttribute("id_prep") ?? "",
                        PrepName = reader.GetAttribute("prep") ?? "",
                        GroupId = reader.GetAttribute("id_group") ?? "unknown_group",
                        GroupName = reader.GetAttribute("group") ?? "Не указана",
                        BuildingName = reader.GetAttribute("buildings") ?? "Не указан",
                        RoomNumber = reader.GetAttribute("rooms") ?? "Не указана",

                        // Парсинг числовых параметров, при отсутствии значения пишет дефолтное: неделя - 0, день - 1, пара - 1 
                        Week = int.TryParse(reader.GetAttribute("week"), out var w) ? w : 0,
                        Day = int.TryParse(reader.GetAttribute("day"), out var d) ? d : 1,
                        Less = int.TryParse(reader.GetAttribute("less"), out var l) ? l : 1
                    };

                    // Бизнес-логика: если идентификатор преподавателя пуст, подставляется заглушка
                    if (string.IsNullOrWhiteSpace(dto.PrepId))
                    {
                        dto.PrepId = "-";
                        dto.PrepName = "Преподаватель не указан";
                    }

                    subjects.Add(dto);

                    if (stopwatch.ElapsedMilliseconds >= ProgressReportIntervalMs && fileLength > 0)
                    {
                        int streamPercent = (int)((stream.Position * 100) / fileLength);
                        int scaledPercent = (streamPercent * MaxReaderProgressPercent) / 100;

                        if (scaledPercent > lastReportedPercent && scaledPercent <= MaxReaderProgressPercent)
                        {
                            lastReportedPercent = scaledPercent;
                            await onProgress.Invoke(scaledPercent, $"Парсинг XML файла... ({scaledPercent}%)");
                        }

                        stopwatch.Restart();
                    }

                }
            }

            return subjects;
        }
    }
}

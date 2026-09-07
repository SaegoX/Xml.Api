using System.Xml;

namespace Xml.Api.Features.ScheduleUpload
{
    /// <summary>
    /// Модуль потокового чтения XML-файла расписания.
    /// Отвечает за парсинг сырого XML в плоские структуры DTO.
    /// </summary>
    public class ScheduleReader
    {
        /// <summary>
        /// Асинхронно читает XML-файл и извлекает из него список объектов расписания.
        /// Использует XmlReader для минимизации потребления оперативной памяти (стриминг).
        /// </summary>
        /// <param name="filePath">Абсолютный путь к XML-файлу на диске сервера</param>
        /// <returns>Коллекция плоских DTO с данными из файла</returns>
        public async Task<IEnumerable<SubjectDto>> ReadAsync(string filePath)
        {
            var subjects = new List<SubjectDto>();

            // Мера безопасности, запрещаем обработку внешних сущностей DTD (защита XML External Entity Attack)
            var settings = new XmlReaderSettings 
            { 
                Async = true, 
                DtdProcessing = DtdProcessing.Prohibit 
            };

            // Открываем файловый поток в асинхронном режиме с заданным размером буфера 4096
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            using var reader = XmlReader.Create(stream, settings);

            while (await reader.ReadAsync())
            {
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
                }
            }

            return subjects;
        }
    }
}

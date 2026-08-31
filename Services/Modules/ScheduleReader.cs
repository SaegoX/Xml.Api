using System.Xml;

namespace Xml.Api.Services.Modules
{
    public class ScheduleReader
    {
        public async Task<IEnumerable<SubjectDto>> ReadAsync(string filePath)
        {
            var subjects = new List<SubjectDto>();
            var settings = new XmlReaderSettings { Async = true, DtdProcessing = DtdProcessing.Prohibit };

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            using var reader = XmlReader.Create(stream, settings);

            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "subject")
                {
                    var dto = new SubjectDto
                    {
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
                        Week = int.TryParse(reader.GetAttribute("week"), out var w) ? w : 0,
                        Day = int.TryParse(reader.GetAttribute("day"), out var d) ? d : 1,
                        Less = int.TryParse(reader.GetAttribute("less"), out var l) ? l : 1
                    };

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

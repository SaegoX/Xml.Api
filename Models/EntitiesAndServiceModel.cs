namespace Xml.Api.Models
{
    /// <summary>
    /// Контейнер моделей представления (DTO) для отдачи расписания в JSON.
    /// </summary>
    public class EntitiesAndServiceModel
    {

        public class RaspFullModel
        {
            public InfoModel? Info { get; set; }
            public FilterModel? Filter { get; set; }
            public IEnumerable<DayModel> Days { get; set; } = Array.Empty<DayModel>();

            public TagSelectModel? RegPreps { get; set; }
            public TagSelectModel? RegGroups { get; set; }
            public TagSelectModel? RegChairs { get; set; }
            public TagSelectModel? RegRooms { get; set; }
        }

        public class RaspSmartModel
        {
            public InfoModel? Info { get; set; }
            public FilterModel? Filter { get; set; }
            public IEnumerable<DayModel> Days { get; set; } = Array.Empty<DayModel>();
        }

        public class InfoModel
        {
            public string Years { get; set; } = string.Empty;
            public bool IsAutumn { get; set; }
            public DateTime DateRelease { get; set; }
            public DateTime DateImport { get; set; }
            public int CurrentWeek { get; set; }
            public bool IsWeekOdd { get; set; }
            public int CurrentDay { get; set; }
            public int CurrentLess { get; set; }
            public int PastLess { get; set; }
            public int UpcomingLess { get; set; }
            public TimeOnly Time { get; set; }
        }

        public class FilterModel
        {
            public string Text { get; set; } = string.Empty;
            public int PrepAisId { get; set; }
            public int GroupAisId { get; set; }
            public int ChairId { get; set; }
            public int RoomId { get; set; }
        }

        public class DayModel
        {
            public int Day { get; set; } // Номер дня (1-7)
            public string Title { get; set; } = string.Empty; // День недели
            public IEnumerable<LessModel> Lessons { get; set; } = Array.Empty<LessModel>();
        }

        public class LessModel
        {
            public int Less { get; set; } // Номер пары (1-7)
            public TimeOnly? Begin { get; set; } // Время начала
            public TimeOnly? End { get; set; } // Время окончания
            public IEnumerable<EventModel> WeekAll { get; set; } = Array.Empty<EventModel>(); // Обе недели
            public IEnumerable<EventModel> Week1 { get; set; } = Array.Empty<EventModel>();   // Чётная
            public IEnumerable<EventModel> Week2 { get; set; } = Array.Empty<EventModel>();   // Нечётная
        }

        public class EventModel
        {
            public int Id { get; set; }
            public string SubgId { get; set; } = string.Empty;
            public int Week { get; set; }
            public string Type { get; set; } = string.Empty; // "Л", "Пр"
            public string Dics { get; set; } = string.Empty; // Название предмета
            public int? ChairId { get; set; }
            public int[] GroupsAisIds { get; set; } = Array.Empty<int>(); // ID групп занятия
            public int[] PrepsAisIds { get; set; } = Array.Empty<int>();  // ID преподов занятия
            public int[] RoomsIds { get; set; } = Array.Empty<int>();     // ID аудиторий занятия
        }

        public class ApiResultModel
        {
            public string Result { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Params { get; set; } = string.Empty;
        }

        public class TagSelectModel
        {
            public TagOptionModel? Selected { get; set; }
            public IEnumerable<TagOptionModel> Options { get; set; } = Array.Empty<TagOptionModel>();
        }

        public class TagOptionModel
        {
            public string Value { get; set; } = string.Empty;
            public string Inner { get; set; } = string.Empty;
            public bool IsSelected { get; set; }
            public int Level { get; set; }
        }
    }
}
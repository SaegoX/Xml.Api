namespace Xml.Api.Features.ScheduleApi
{
    public class ScheduleViewModels
    {
        /// <summary>
        /// Модель мета-информации о текущем состоянии расписания и времени семестра.
        /// </summary>
        public class InfoModel
        {
            public string Years { get; set; } = string.Empty; 
            public bool IsAutumn { get; set; }
            public DateTime DateRelease { get; set; }
            public DateTime DateImport { get; set; }
            public int CurrentWeek { get; set; }
            public bool IsWeekOdd { get; set; }
            public int CurrentDay { get; set; }
            public string Time { get; set; } = string.Empty;
            public int CurrentLess { get; set; }
            public int PastLess { get; set; }
            public int UpcomingLess { get; set; }
        }

        /// <summary>
        /// Конфигурационная модель одной учебной пары (расписание звонков) из appsettings.json.
        /// </summary>
        public class LessonTimeConfig
        {
            public string Begin { get; set; } = string.Empty;
            public string End { get; set; } = string.Empty;
        }

        public class PrepViewModel
        {
            public int Id { get; set; }
            public string? AisId { get; set; }
            public string Fio { get; set; } = string.Empty;
            public string? Degree { get; set; }
        }

        public class GroupViewModel
        {
            public int Id { get; set; }
            public string? AisId { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class ChairViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
        }

        /// <summary>
        /// Модель представления здания (корпуса) для API.
        /// </summary>
        public class BuildingViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public List<RoomInnerModel> Rooms { get; set; } = new(); // Вложенный список комнат корпуса
        }

        /// <summary>
        /// Вложенная модель конкретной аудитории внутри здания.
        /// </summary>
        public class RoomInnerModel
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty; 
        }

        public class DayModel
        {
            public int Day { get; set; } // Номер дня недели 
            public string Title { get; set; } = string.Empty; 
            public List<LessModel> Lessons { get; set; } = new();
        }

        public class LessModel
        {
            public int Less { get; set; } 
            public string? Begin { get; set; } 
            public string? End { get; set; } 
            public List<EventModel> WeekAll { get; set; } = new();
            public List<EventModel> Week1 { get; set; } = new(); 
            public List<EventModel> Week2 { get; set; } = new(); 
        }

        public class EventModel
        {
            public int Id { get; set; }
            public string SubgId { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty; // "Л", "Пр", "Лаб"
            public int Week { get; set; }
            public string DiscName { get; set; } = string.Empty;
            public string ChairName { get; set; } = string.Empty;
            public string Groups { get; set; } = string.Empty; 
            public string Preps { get; set; } = string.Empty; 
            public string Rooms { get; set; } = string.Empty;  
        }
    }
}

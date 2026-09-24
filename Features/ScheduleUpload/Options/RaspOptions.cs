namespace Xml.Api.Features.ScheduleUpload.Options
{
    /// <summary>
    /// Класс абстракции для вытаскивания опции нашего расписания
    /// </summary>
    public class RaspOptions
    {
        public string Title { get; set; } = string.Empty;
        public int WeekOffset { get; set; }
        public List<LessonTimeConfig> Lessons { get; set; } = new();
    }

    /// <summary>
    /// Конфигурационная модель одной пары (звонка) из appsettings.json
    /// </summary>
    public class LessonTimeConfig
    {
        public string Begin { get; set; } = string.Empty;
        public string End { get; set; } = string.Empty;
    }
}
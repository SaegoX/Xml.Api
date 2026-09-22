using static Xml.Api.Features.ScheduleApi.ScheduleViewModels;

namespace Xml.Api.Features.ScheduleApi
{
    public class RaspOptions
    {
        public string Title { get; set; } = string.Empty;
        public int WeekOffset { get; set; }
        public List<LessonTimeConfig> Lessons { get; set; } = new();

    }
}
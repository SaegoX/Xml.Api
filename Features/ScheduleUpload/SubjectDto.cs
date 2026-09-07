namespace Xml.Api.Features.ScheduleUpload
{
    public class SubjectDto
    {
        public string IdSubg { get; set; } = string.Empty;
        public string DiscName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string ChairName { get; set; } = string.Empty;
        public string PrepId { get; set; } = string.Empty;
        public string PrepName { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public int Week { get; set; }
        public int Day { get; set; }
        public int Less { get; set; }
        public string BuildingName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
    }
}

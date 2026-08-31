using System.ComponentModel.DataAnnotations;

namespace Xml.Api.Models
{
    public class EventToGroupRef
    {
        [Key]
        public int Id { get; set; }
        public int MasterPtr { get; set; }
        public Event Event { get; set; } = null!;
        public int GroupPtr { get; set; }
        public Group Group { get; set; } = null!;
    }

    public class EventToPrepRef
    {
        [Key]
        public int Id { get; set; }
        public int MasterPtr { get; set; }
        public Event Event { get; set; } = null!;
        public int PrepPtr { get; set; }
        public Prep Prep { get; set; } = null!;
    }

    public class EventToBuildingRoomRef
    {
        [Key]
        public int Id { get; set; }
        public int MasterPtr { get; set; }
        public Event Event { get; set; } = null!;
        public int BuildingRoomPtr { get; set; }
        public BuildingRoom Room { get; set; } = null!;
    }
}

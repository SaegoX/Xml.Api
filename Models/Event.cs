using System.ComponentModel.DataAnnotations;

namespace Xml.Api.Models
{
    public class Event
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SubgId { get; set; } = string.Empty;

        public int Day { get; set; }
        public int Less { get; set; }
        public int Week { get; set; }

        [Required]
        public string Type { get; set; } = string.Empty;

        public int? DiscId { get; set; }
        public Disc? Disc { get; set; }

        public int? ChairId { get; set; }
        public Chair? Chair { get; set; }

        public string? Prop { get; set; }

        public ICollection<EventToGroupRef> GroupRefs { get; set; } = new List<EventToGroupRef>();
        public ICollection<EventToPrepRef> PrepRefs { get; set; } = new List<EventToPrepRef>();
        public ICollection<EventToBuildingRoomRef> RoomRefs { get; set; } = new List<EventToBuildingRoomRef>();
    }
}

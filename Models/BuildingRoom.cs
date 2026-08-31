using System.ComponentModel.DataAnnotations;

namespace Xml.Api.Models
{
    public class BuildingRoom
    {
        [Key]
        public int Id { get; set; }

        public int MasterPtr {  get; set; }
        public Building Building { get; set; } = null!;

        [Required]
        public string Name { get; set; } = string.Empty;

        public ICollection<EventToBuildingRoomRef> EventRefs { get; set; } = new List<EventToBuildingRoomRef>();
    }
}

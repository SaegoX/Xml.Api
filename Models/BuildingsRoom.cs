using System.ComponentModel.DataAnnotations;

namespace Xml.Api.Models
{
    public class BuildingsRoom
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        [Required]
        public string RoomNumber { get; set; } = string.Empty;

        [Required]
        public string BuildingName { get; set; } = string.Empty;
        public Building Building { get; set; } = null!;

        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}

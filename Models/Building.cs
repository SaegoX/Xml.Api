using System.ComponentModel.DataAnnotations;

namespace Xml.Api.Models
{
    public class Building
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public ICollection<BuildingRoom> Rooms { get; set; } = new List<BuildingRoom>();
    }
}

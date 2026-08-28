using System.ComponentModel.DataAnnotations;

namespace Xml.Api.Models
{
    public class Building
    {
        [Key]
        public string Name { get; set; } = string.Empty;

        public ICollection<BuildingsRoom> Rooms { get; set; } = new List<BuildingsRoom>();
    }
}

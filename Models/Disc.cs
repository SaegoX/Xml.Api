using System.ComponentModel.DataAnnotations;

namespace Xml.Api.Models
{
    public class Disc
    {
        [Key]
        public string Name { get; set; } = string.Empty;

        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}

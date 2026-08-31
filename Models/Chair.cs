using System.ComponentModel.DataAnnotations;

namespace Xml.Api.Models
{
    public class Chair
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;
        public string? Prop {  get; set; }

        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}

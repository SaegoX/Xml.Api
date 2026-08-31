using System.ComponentModel.DataAnnotations;

namespace Xml.Api.Models
{
    public class Prep
    {
        [Key]
        public int Id { get; set; }

        public string? AisId { get; set; }
        
        [Required]
        public string FullName { get; set; } = string.Empty;
        public string? Degree { get; set; }
        public string? Data { get; set; }
        public string? Prop { get; set; }

        public ICollection<EventToPrepRef> EventRefs { get; set; } = new List<EventToPrepRef>();
    }
}

using System.ComponentModel.DataAnnotations;

namespace Xml.Api.Models
{
    public class Group
    {
        [Key]
        public int Id { get; set; }

        public string? AisId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Prop { get; set;  }

        public ICollection<EventToGroupRef> EventRefs { get; set; } = new List<EventToGroupRef>();
    }
}

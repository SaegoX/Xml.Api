using System.ComponentModel.DataAnnotations;

namespace Xml.Api.Models
{
    public class Settings
    {
        [Key]
        public int id { get; set; } 
        public DateTime? DateRelease { get; set; }
        public DateTime? DateImport { get; set; }
    }
}

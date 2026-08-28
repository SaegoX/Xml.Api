using System.ComponentModel.DataAnnotations;

namespace Xml.Api.Models
{
    public class Settings
    {
        [Key]
        public string Key { get; set; } = string.Empty; 

        public string Value { get; set; } = string.Empty; 
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xml.Api.Models
{
    public class Event
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string IdSubg { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty;

        public int Week { get; set; }
        public int Day { get; set; } 
        public int Less { get; set; } 

        public string? PrepId { get; set; }
        public Prep? Prep { get; set; }

        public string? GroupId { get; set; }
        public Group? Group { get; set; }

        public string? DiscName { get; set; }
        public Disc? Disc { get; set; }

        public string? ChairName { get; set; }
        public Chair? Chair { get; set; }

        public string? RoomId { get; set; }
        public BuildingsRoom? Room { get; set; }
    }
}

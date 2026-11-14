using System.ComponentModel.DataAnnotations;

namespace RaceDayMVC.Models
{
    public class EventViewModel
    {
        public int EventID { get; set; }

        [Required(ErrorMessage = "Event name is required.")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(255)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Event date is required.")]
        [DataType(DataType.Date)]
        public DateTime EventDate { get; set; } = DateTime.Today.AddDays(7);

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(150)]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Distance is required.")]
        [Range(0.1, 9999)]
        public decimal Distance { get; set; }

        [Required(ErrorMessage = "Please select an event type.")]
        public string EventType { get; set; } = string.Empty;

        public IFormFile? BannerImage { get; set; }

        public string? BannerImageUrl { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace RaceDayMVC.Models
{
    public class CategoryViewModel
    {
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Please select an event.")]
        public int EventID { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [Range(1, 100, ErrorMessage = "Minimum age must be between 1 and 100.")]
        public int? MinimumAge { get; set; }

        [Range(1, 100, ErrorMessage = "Maximum age must be between 1 and 100.")]
        public int? MaximumAge { get; set; }

        [Range(typeof(decimal), "0.1", "9999",
            ErrorMessage = "Distance must be greater than zero.")]
        public decimal? Distance { get; set; }
    }
}
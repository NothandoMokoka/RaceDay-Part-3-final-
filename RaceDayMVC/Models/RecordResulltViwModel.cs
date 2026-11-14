using System.ComponentModel.DataAnnotations;

namespace RaceDayMVC.Models
{
    public class RecordResultViewModel
    {
        [Required(ErrorMessage = "Please select an enrolment.")]
        public int EnrollmentID { get; set; }

        [Required]
        [Range(1, 10000, ErrorMessage = "Enter a valid finishing position.")]
        public int FinishPosition { get; set; }

        [Required]
        public TimeSpan FinishTime { get; set; }
    }
}
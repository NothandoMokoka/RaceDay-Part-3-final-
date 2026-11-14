using System.ComponentModel.DataAnnotations;

namespace RaceDayMVC.Models
{
    public class ParticipantProfileViewModel
    {
        public int UserID { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        public string? ProfileImageUrl { get; set; }

        public IFormFile? ProfileImage { get; set; }
    }
}
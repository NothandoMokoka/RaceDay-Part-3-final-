namespace RaceDayMVC.Models
{
    public class OrganizerResultViewModel
    {
        public int ResultID { get; set; }
        public int EnrollmentID { get; set; }
        public int FinishPosition { get; set; }
        public TimeSpan FinishTime { get; set; }
        public DateTime RecordedAt { get; set; }

        public string ParticipantName { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
    }
}
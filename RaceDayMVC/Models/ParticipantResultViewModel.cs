namespace RaceDayMVC.Models
{
    public class ParticipantResultViewModel
    {
        public int ResultID { get; set; }
        public int EnrollmentID { get; set; }
        public int FinishPosition { get; set; }
        public TimeSpan FinishTime { get; set; }
        public DateTime RecordedAt { get; set; }

        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}
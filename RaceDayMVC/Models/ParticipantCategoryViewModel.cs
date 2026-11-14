namespace RaceDayMVC.Models
{
    public class ParticipantCategoryViewModel
    {
        public int CategoryID { get; set; }
        public int EventID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int? MinimumAge { get; set; }
        public int? MaximumAge { get; set; }
        public decimal? Distance { get; set; }
    }
}
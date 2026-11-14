namespace RaceDayMVC.Models
{
    public class OrganizerCategoryViewModel
    {
        public int CategoryID { get; set; }

        public int EventID { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public int? MinimumAge { get; set; }

        public int? MaximumAge { get; set; }

        public decimal? Distance { get; set; }

        public string EventName { get; set; } = string.Empty;
    }
}
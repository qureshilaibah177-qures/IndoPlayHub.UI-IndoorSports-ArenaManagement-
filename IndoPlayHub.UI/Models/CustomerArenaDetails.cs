namespace IndoPlayHub.UI.Models
{
    public class CustomerArenaDetails
    {
        // =========================================================
        // ARENA
        // =========================================================

        public int ArenaID { get; set; }

        public string ArenaName { get; set; } =
            string.Empty;

        public string Location { get; set; } =
            string.Empty;

        public string CoverImageUrl { get; set; } =
            "arena_bg.jpg";


        // =========================================================
        // CONTACT
        // =========================================================

        public string PhoneNumber { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;


        // =========================================================
        // BUSINESS HOURS
        // =========================================================

        public TimeSpan OpeningTime { get; set; }

        public TimeSpan ClosingTime { get; set; }


        // =========================================================
        // RATING
        // =========================================================

        public double Rating { get; set; }

        public int ReviewCount { get; set; }


        // =========================================================
        // CUSTOMER-FACING DESCRIPTION
        // =========================================================

        public string Description { get; set; } =
            string.Empty;


        // =========================================================
        // FACILITIES
        // =========================================================

        public List<string> Facilities { get; set; } =
            new();


        // =========================================================
        // COURTS / SPORTS
        // =========================================================

        public List<CustomerArenaCourtItem> Courts { get; set; } =
            new();


        // =========================================================
        // DISPLAY PROPERTIES
        // =========================================================

        public string RatingText =>
            Rating > 0
                ? $"⭐ {Rating:0.0}"
                : "No rating";


        public string ReviewText =>
            $"{ReviewCount} reviews";


        public string OpeningHoursText =>
            $"Open {FormatTime(OpeningTime)} – {FormatTime(ClosingTime)}";


        private static string FormatTime(
            TimeSpan time)
        {
            return DateTime.Today
                .Add(time)
                .ToString("h:mm tt");
        }
    }


    // =============================================================
    // CUSTOMER COURT DISPLAY MODEL
    // =============================================================

    public class CustomerArenaCourtItem
    {
        public int ItemID { get; set; }


        public string ItemName { get; set; } =
            string.Empty;


        public string CategoryName { get; set; } =
            string.Empty;


        public decimal PricePerHour { get; set; }


        public bool IsAvailable { get; set; }


        // =========================================================
        // PRICE DISPLAY
        // =========================================================

        public string PriceText =>
            PricePerHour > 0
                ? $"Rs. {PricePerHour:N0}/hr"
                : "Price unavailable";


        // =========================================================
        // AVAILABILITY DISPLAY
        // =========================================================

        public string AvailabilityText =>
            IsAvailable
                ? "Available"
                : "Currently unavailable";


        public string AvailabilitySymbol =>
            IsAvailable
                ? "✓"
                : "•";
    }
}
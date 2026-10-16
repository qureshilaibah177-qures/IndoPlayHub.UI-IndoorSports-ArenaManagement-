
namespace IndoPlayHub.UI.Models
{
    public class CustomerBookingConfirmation
    {
        // =========================================================
        // ARENA
        // =========================================================

        public int ArenaID { get; set; }

        public string ArenaName { get; set; } =
            string.Empty;

        public string Location { get; set; } =
            string.Empty;


        // =========================================================
        // COURT / SPORT
        // =========================================================

        public int ItemID { get; set; }

        public string CourtName { get; set; } =
            string.Empty;

        public string SportName { get; set; } =
            string.Empty;


        // =========================================================
        // CUSTOMER
        // =========================================================

        public int CustomerID { get; set; }

        public string CustomerName { get; set; } =
            string.Empty;

        public string CustomerPhone { get; set; } =
            string.Empty;


        // =========================================================
        // DATE & TIME
        // =========================================================

        public DateOnly BookingDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }


        // =========================================================
        // PRICE
        // =========================================================

        public decimal PricePerHour { get; set; }


        // =========================================================
        // DISPLAY PROPERTIES
        // =========================================================

        public string BookingDateText =>
            BookingDate.ToString("dddd, dd MMMM yyyy");

        public string TimeText =>
            $"{DateTime.Today.Add(StartTime):h:mm tt} - " +
            $"{DateTime.Today.Add(EndTime):h:mm tt}";

        public string PriceText =>
            $"Rs. {PricePerHour:N0}";

        public string LocationText =>
            $"📍 {Location}";

        public string DurationText =>
            $"{(EndTime - StartTime).TotalHours:0.##} hour";
    }
}


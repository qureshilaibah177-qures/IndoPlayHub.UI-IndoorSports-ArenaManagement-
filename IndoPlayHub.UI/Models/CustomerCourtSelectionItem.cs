namespace IndoPlayHub.UI.Models
{
    public class CustomerCourtSelectionItem
    {
        public int ItemID { get; set; }

        public int ArenaID { get; set; }

        public int CategoryID { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public decimal PricePerHour { get; set; }

        public bool IsAvailable { get; set; }

        public string? ImageUrl { get; set; }

        public string CategoryName { get; set; } = string.Empty;


        // =====================================================
        // DISPLAY PROPERTIES
        // =====================================================

        public string PriceText =>
            $"Rs. {PricePerHour:N0} / hour";


        public string PriceShortText =>
            $"Rs. {PricePerHour:N0}";


        public string AvailabilityText =>
            IsAvailable
                ? "🟢 Available"
                : "🔴 Booked";


        public string AvailabilityDetailText =>
            IsAvailable
                ? "This court is currently available for booking."
                : "This court is currently unavailable.";


        public string AvailabilityColor =>
            IsAvailable
                ? "#03DAC6"
                : "#FF5C5C";


        public string ButtonText =>
            IsAvailable
                ? "Select Court"
                : "Unavailable";


        public string ButtonBackgroundColor =>
            IsAvailable
                ? "#03DAC6"
                : "#292D35";


        public string ButtonTextColor =>
            IsAvailable
                ? "#101114"
                : "#777D87";
    }
}
namespace IndoPlayHub.UI.Models
{
    public class CustomerArenaItem
    {
        // =========================================================
        // ARENA
        // =========================================================

        public int ArenaID { get; set; }

        public string ArenaName { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;


        // =========================================================
        // IMAGE
        // =========================================================

        public string CoverImageUrl { get; set; } = "arena_bg.jpg";


        // =========================================================
        // SPORT
        // =========================================================

        public int CategoryID { get; set; }

        public string SportName { get; set; } = string.Empty;


        // =========================================================
        // UNITS
        // =========================================================

        /// <summary>
        /// Total number of courts/units offered by this arena
        /// for the selected sport.
        /// </summary>
        public int TotalUnits { get; set; }


        /// <summary>
        /// Number of currently available courts/units.
        /// </summary>
        public int AvailableUnits { get; set; }


        // =========================================================
        // PRICE
        // =========================================================

        public decimal StartingPrice { get; set; }


        // =========================================================
        // RATING
        // =========================================================

        public double Rating { get; set; }


        // =========================================================
        // DISTANCE
        // =========================================================

        public double DistanceKm { get; set; }


        // =========================================================
        // OPEN / CLOSED STATUS
        // =========================================================

        public string OpenStatus { get; set; } = string.Empty;


        // =========================================================
        // PRICE DISPLAY
        // =========================================================

        public string PriceText
        {
            get
            {
                return StartingPrice > 0
                    ? $"From Rs. {StartingPrice:N0} / hour"
                    : "Price unavailable";
            }
        }


        // =========================================================
        // UNIT DISPLAY
        // =========================================================

        /// <summary>
        /// Example:
        /// 1 unit • Available
        /// 1 unit • Currently unavailable
        /// 3 units • 3 available
        /// 5 units • 2 available
        /// </summary>
        public string UnitText
        {
            get
            {
                if (TotalUnits <= 0)
                    return "No units available";

                if (TotalUnits == 1)
                {
                    return AvailableUnits > 0
                        ? "1 unit • Available"
                        : "1 unit • Currently unavailable";
                }

                return $"{TotalUnits} units • {AvailableUnits} available";
            }
        }


        // =========================================================
        // SHORT AVAILABILITY DISPLAY
        // =========================================================

        public string AvailabilityText
        {
            get
            {
                if (AvailableUnits <= 0)
                    return "Fully booked";

                if (AvailableUnits == 1)
                    return "1 available";

                return $"{AvailableUnits} available";
            }
        }


        // =========================================================
        // RATING DISPLAY
        // =========================================================

        public string RatingText
        {
            get
            {
                return Rating > 0
                    ? $"⭐ {Rating:0.0}"
                    : "No rating";
            }
        }


        // =========================================================
        // DISTANCE DISPLAY
        // =========================================================

        public string DistanceText
        {
            get
            {
                return DistanceKm > 0
                    ? $"{DistanceKm:0.0} km away"
                    : "Distance unavailable";
            }
        }


        // =========================================================
        // AVAILABILITY BOOLEAN
        // =========================================================

        public bool IsAvailable =>
            AvailableUnits > 0;


        // =========================================================
        // BACKWARD COMPATIBILITY
        // =========================================================
        // Your existing XAML/code uses these names.
        // Keep them so nothing else breaks.
        // =========================================================

        public int AvailableCourts =>
            AvailableUnits;


        public string CourtText =>
            UnitText;
    }
}
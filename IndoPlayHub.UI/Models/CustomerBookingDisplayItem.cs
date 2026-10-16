namespace IndoPlayHub.UI.Models
{
    public class CustomerBookingDisplayItem
    {
        public int BookingID { get; set; }

        public int ArenaID { get; set; }

        public int ItemID { get; set; }

        public string ArenaName { get; set; } =
            string.Empty;

        public string SportName { get; set; } =
            string.Empty;

        public string CourtName { get; set; } =
            string.Empty;

        public string BookingDateText { get; set; } =
            string.Empty;

        public string BookingTimeText { get; set; } =
            string.Empty;

        public decimal TotalAmount { get; set; }

        public string TotalAmountText =>
            $"Rs. {TotalAmount:N0}";

        public string BookingStatus { get; set; } =
            string.Empty;

        public string PaymentStatus { get; set; } =
            string.Empty;

        public string PaymentMethod { get; set; } =
            string.Empty;

        public string CancellationReason { get; set; } =
            string.Empty;

        public string RefundStatus { get; set; } =
            string.Empty;

        public bool IsUpcoming { get; set; }

        public bool IsCompleted { get; set; }

        public bool IsCancelled { get; set; }

        public string StatusText
        {
            get
            {
                if (IsCancelled)
                    return "Cancelled";

                if (IsCompleted)
                    return "Completed";

                return "Upcoming";
            }
        }
    }
}
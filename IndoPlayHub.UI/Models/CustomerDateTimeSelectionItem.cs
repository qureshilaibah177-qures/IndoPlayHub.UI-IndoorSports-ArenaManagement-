using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IndoPlayHub.UI.Models
{
    public class CustomerDateTimeSelectionItem : INotifyPropertyChanged
    {
        // =========================================================
        // DATE & TIME
        // =========================================================

        public DateOnly BookingDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }


        // =========================================================
        // PRICE & AVAILABILITY
        // =========================================================

        public decimal PricePerHour { get; set; }

        public bool IsAvailable { get; set; }


        // =========================================================
        // DISPLAY
        // =========================================================

        public string TimeText =>
            $"{DateTime.Today.Add(StartTime):hh\\:mm tt} - " +
            $"{DateTime.Today.Add(EndTime):hh\\:mm tt}";

        public string PriceText =>
            $"Rs. {PricePerHour:N0} / hour";

        public string AvailabilityText =>
            IsAvailable
                ? "🟢 Available"
                : "🔴 Booked";

        public string AvailabilityDetailText =>
            IsAvailable
                ? "This hourly slot is available."
                : "This hourly slot is already booked or unavailable.";

        public string ButtonText =>
            IsAvailable
                ? "Select Slot"
                : "Unavailable";


        // =========================================================
        // COLORS
        // =========================================================

        public Color AvailabilityColor =>
            IsAvailable
                ? Color.FromArgb("#03DAC6")
                : Color.FromArgb("#FF5252");

        public Color ButtonBackgroundColor =>
            IsAvailable
                ? Color.FromArgb("#03DAC6")
                : Color.FromArgb("#292D35");

        public Color ButtonTextColor =>
            IsAvailable
                ? Color.FromArgb("#101114")
                : Color.FromArgb("#8F96A3");


        // =========================================================
        // PROPERTY CHANGED
        // =========================================================

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
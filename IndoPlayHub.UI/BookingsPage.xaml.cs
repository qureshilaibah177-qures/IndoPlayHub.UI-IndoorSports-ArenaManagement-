
using IndoPlayHub.UI.Data;
using Microsoft.EntityFrameworkCore;

namespace IndoPlayHub.UI
{
    public partial class BookingsPage : ContentPage
    {
        private readonly AppDbContext _dbContext;
        private readonly int _currentArenaId;

        private bool _isLoading;

        public BookingsPage(
            AppDbContext dbContext,
            int arenaId)
        {
            InitializeComponent();

            _dbContext = dbContext;
            _currentArenaId = arenaId;
        }

        // =========================================================
        // PAGE APPEARS
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await LoadBookingsAsync();
        }

        // =========================================================
        // LOAD BOOKINGS
        // =========================================================

        private async Task LoadBookingsAsync()
        {
            if (_isLoading)
                return;

            _isLoading = true;

            try
            {
                if (_currentArenaId <= 0)
                {
                    await DisplayAlertAsync(
                        "Booking Error",
                        "Invalid arena ID.",
                        "OK");

                    return;
                }

                // =================================================
                // DATE RANGE
                // =================================================

                // Booking.BookingDate is DateOnly.
                // Therefore today and tomorrow must also be DateOnly.

                DateOnly today =
                    DateOnly.FromDateTime(DateTime.Today);

                DateOnly tomorrow =
                    today.AddDays(1);

                // =================================================
                // LOAD BOOKINGS
                // =================================================

                var bookings =
                    await _dbContext.Bookings
                        .AsNoTracking()
                        .Include(b => b.CourtOrMenuItem)
                        .Where(b =>
                            b.ArenaID == _currentArenaId)
                        .OrderByDescending(b => b.BookingDate)
                        .ThenBy(b => b.StartTime)
                        .ToListAsync();

                // =================================================
                // SUMMARY
                // =================================================

                var todayBookings =
                    bookings
                        .Where(b =>
                            b.BookingDate >= today &&
                            b.BookingDate < tomorrow)
                        .ToList();

                // -------------------------------------------------
                // TODAY'S BOOKINGS
                // -------------------------------------------------

                LblTodayBookings.Text =
                    todayBookings.Count.ToString();

                // -------------------------------------------------
                // TODAY'S REVENUE
                // -------------------------------------------------

                decimal todayRevenue =
                    todayBookings
                        .Where(b =>
                            string.Equals(
                                b.BookingStatus,
                                "Confirmed",
                                StringComparison.OrdinalIgnoreCase))
                        .Sum(b => b.TotalAmount);

                LblTodayRevenue.Text =
                    $"Rs. {todayRevenue:N0}";

                // -------------------------------------------------
                // PENDING PAYMENTS
                // -------------------------------------------------

                LblPendingPayments.Text =
                    todayBookings
                        .Count(b =>
                            string.Equals(
                                b.PaymentStatus,
                                "Pending",
                                StringComparison.OrdinalIgnoreCase))
                        .ToString();

                // -------------------------------------------------
                // CONFIRMED BOOKINGS
                // -------------------------------------------------

                LblConfirmedBookings.Text =
                    todayBookings
                        .Count(b =>
                            string.Equals(
                                b.BookingStatus,
                                "Confirmed",
                                StringComparison.OrdinalIgnoreCase))
                        .ToString();

                // =================================================
                // BOOKING LIST
                // =================================================

                var displayBookings =
                    bookings
                        .Select(b => new BookingDisplayItem
                        {
                            BookingID =
                                b.BookingID,

                            CustomerName =
                                b.CustomerName,

                            CustomerPhone =
                                b.CustomerPhone,

                            CourtName =
                                b.CourtOrMenuItem?.ItemName
                                ?? "Unknown Court",

                            TimeRange =
                                $"{b.StartTime:hh\\:mm} - " +
                                $"{b.EndTime:hh\\:mm}",

                            TotalAmount =
                                b.TotalAmount,

                            BookingStatus =
                                b.BookingStatus,

                            PaymentStatus =
                                b.PaymentStatus,

                            PaymentColor =
                                GetPaymentColor(
                                    b.PaymentStatus)

                        })
                        .ToList();

                CvBookings.ItemsSource =
                    displayBookings;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Booking Error",
                    $"Could not load bookings:\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                _isLoading = false;
            }
        }

        // =========================================================
        // PAYMENT COLOR
        // =========================================================

        private Color GetPaymentColor(string? status)
        {
            if (string.Equals(
                status,
                "Paid",
                StringComparison.OrdinalIgnoreCase))
            {
                return Color.FromArgb("#03DAC6");
            }

            if (string.Equals(
                status,
                "Pending",
                StringComparison.OrdinalIgnoreCase))
            {
                return Color.FromArgb("#FFB74D");
            }

            if (string.Equals(
                status,
                "Failed",
                StringComparison.OrdinalIgnoreCase))
            {
                return Color.FromArgb("#FF5252");
            }

            return Color.FromArgb("#9AA4B2");
        }

        // =========================================================
        // ADD BOOKING
        // =========================================================

        private async void OnAddBookingClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                if (_currentArenaId <= 0)
                {
                    await DisplayAlertAsync(
                        "Booking Error",
                        "Invalid arena ID.",
                        "OK");

                    return;
                }

                await Navigation.PushAsync(
                    new AddBookingPage(
                        _dbContext,
                        _currentArenaId));
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Navigation Error",
                    $"Could not open the booking form:\n\n{ex.Message}",
                    "OK");
            }
        }

        // =========================================================
        // BOOKING DISPLAY MODEL
        // =========================================================

        private class BookingDisplayItem
        {
            public int BookingID { get; set; }

            public string CustomerName { get; set; } =
                string.Empty;

            public string CustomerPhone { get; set; } =
                string.Empty;

            public string CourtName { get; set; } =
                string.Empty;

            public string TimeRange { get; set; } =
                string.Empty;

            public decimal TotalAmount { get; set; }

            public string BookingStatus { get; set; } =
                string.Empty;

            public string PaymentStatus { get; set; } =
                string.Empty;

            public Color PaymentColor { get; set; } =
                Color.FromArgb("#9AA4B2");
        }
    }
}


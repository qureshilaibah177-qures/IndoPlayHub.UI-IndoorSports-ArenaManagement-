using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using Microsoft.EntityFrameworkCore;

namespace IndoPlayHub.UI
{
    public partial class CustomerBookingConfirmationPage :
        ContentPage,
        IQueryAttributable
    {
        // =========================================================
        // DATABASE
        // =========================================================

        private readonly AppDbContext _dbContext;

        // =========================================================
        // BOOKING
        // =========================================================

        private int _bookingID;

        // =========================================================
        // STATE
        // =========================================================

        private bool _queryReceived;
        private bool _isLoading;
        private bool _pageInitialized;

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public CustomerBookingConfirmationPage(
            AppDbContext dbContext)
        {
            InitializeComponent();

            _dbContext = dbContext
                ?? throw new ArgumentNullException(nameof(dbContext));

            _pageInitialized = true;

            ResetDisplay();
        }

        // =========================================================
        // PAGE APPEARING
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                if (!_pageInitialized)
                    return;

                // Query loading is initiated from
                // ApplyQueryAttributes().
                //
                // Do not reload the booking every time
                // the page appears.
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"CustomerBookingConfirmationPage OnAppearing error: {ex}");
            }
        }

        // =========================================================
        // RECEIVE QUERY
        // =========================================================

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            try
            {
                if (_queryReceived)
                    return;

                if (query == null || query.Count == 0)
                {
                    _queryReceived = false;

                    _ = ShowErrorAsync(
                        "Booking Confirmation",
                        "No booking information was received.");

                    return;
                }

                // =================================================
                // READ BOOKING ID
                // =================================================

                int bookingID =
                    GetIntValue(
                        query,
                        "BookingID");

                // =================================================
                // VALIDATE
                // =================================================

                if (bookingID <= 0)
                {
                    _queryReceived = false;

                    _ = ShowErrorAsync(
                        "Booking Confirmation",
                        "The booking ID received from the previous page is invalid.");

                    return;
                }

                // =================================================
                // STORE
                // =================================================

                _bookingID = bookingID;

                _queryReceived = true;

                // =================================================
                // LOAD
                // =================================================

                _ = LoadBookingSafelyAsync();
            }
            catch (Exception ex)
            {
                _queryReceived = false;

                System.Diagnostics.Debug.WriteLine(
                    $"ApplyQueryAttributes failed: {ex}");

                _ = ShowErrorAsync(
                    "Booking Confirmation",
                    "Unable to read the booking confirmation information.");
            }
        }

        // =========================================================
        // SAFE LOAD
        // =========================================================

        private async Task LoadBookingSafelyAsync()
        {
            if (_isLoading)
                return;

            _isLoading = true;

            try
            {
                await LoadBookingAsync();
            }
            catch (OperationCanceledException)
            {
                // Safe cancellation.
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"LoadBookingSafelyAsync failed: {ex}");

                await ShowErrorAsync(
                    "Booking Confirmation",
                    "Unable to load the booking confirmation.");
            }
            finally
            {
                _isLoading = false;
            }
        }

        // =========================================================
        // LOAD SAVED BOOKING
        // =========================================================

        private async Task LoadBookingAsync()
        {
            if (!_queryReceived)
                return;

            if (_bookingID <= 0)
            {
                await ShowErrorAsync(
                    "Booking",
                    "Invalid booking ID.");

                return;
            }

            // =====================================================
            // LOAD BOOKING
            // =====================================================

            Booking? booking =
                await _dbContext.Bookings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        b => b.BookingID == _bookingID);

            if (booking == null)
            {
                await ShowErrorAsync(
                    "Booking",
                    "The confirmed booking could not be found.");

                return;
            }

            // =====================================================
            // LOAD ARENA
            // =====================================================

            Arena? arena =
                await _dbContext.Arenas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        a => a.ArenaID == booking.ArenaID);

            if (arena == null)
            {
                await ShowErrorAsync(
                    "Arena",
                    "The arena associated with this booking could not be found.");

                return;
            }

            // =====================================================
            // LOAD COURT
            // =====================================================

            CourtOrMenuItem? court =
                await _dbContext.CourtsAndMenuItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        c =>
                            c.ItemID == booking.ItemID &&
                            c.ArenaID == booking.ArenaID);

            if (court == null)
            {
                await ShowErrorAsync(
                    "Court",
                    "The court associated with this booking could not be found.");

                return;
            }

            // =====================================================
            // LOAD CATEGORY
            // =====================================================

            Category? category =
                await _dbContext.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        c => c.CategoryID == court.CategoryID);

            // =====================================================
            // DISPLAY BOOKING ID
            // =====================================================

            if (LblBookingID != null)
            {
                LblBookingID.Text =
                    $"#{booking.BookingID}";
            }

            // =====================================================
            // DISPLAY ARENA
            // =====================================================

            if (LblArenaName != null)
            {
                LblArenaName.Text =
                    string.IsNullOrWhiteSpace(arena.ArenaName)
                        ? "Arena"
                        : arena.ArenaName.Trim();
            }

            // =====================================================
            // DISPLAY LOCATION
            // =====================================================

            if (LblLocation != null)
            {
                LblLocation.Text =
                    string.IsNullOrWhiteSpace(arena.Location)
                        ? "Location unavailable"
                        : $"📍 {arena.Location.Trim()}";
            }

            // =====================================================
            // DISPLAY SPORT
            // =====================================================

            if (LblSport != null)
            {
                LblSport.Text =
                    string.IsNullOrWhiteSpace(
                        category?.CategoryName)
                        ? "Sport"
                        : category.CategoryName.Trim();
            }

            // =====================================================
            // DISPLAY COURT
            // =====================================================

            if (LblCourt != null)
            {
                LblCourt.Text =
                    string.IsNullOrWhiteSpace(court.ItemName)
                        ? "Court"
                        : court.ItemName.Trim();
            }

            // =====================================================
            // DISPLAY DATE
            // =====================================================

            if (LblBookingDate != null)
            {
                LblBookingDate.Text =
                    booking.BookingDate.ToString(
                        "dddd, dd MMMM yyyy");
            }

            // =====================================================
            // DISPLAY TIME
            // =====================================================

            if (LblBookingTime != null)
            {
                LblBookingTime.Text =
                    $"{FormatTime(booking.StartTime)} - " +
                    $"{FormatTime(booking.EndTime)}";
            }

            // =====================================================
            // DISPLAY CUSTOMER
            // =====================================================

            if (LblCustomerName != null)
            {
                LblCustomerName.Text =
                    string.IsNullOrWhiteSpace(
                        booking.CustomerName)
                        ? "Customer"
                        : booking.CustomerName.Trim();
            }

            if (LblCustomerPhone != null)
            {
                LblCustomerPhone.Text =
                    string.IsNullOrWhiteSpace(
                        booking.CustomerPhone)
                        ? "Phone unavailable"
                        : booking.CustomerPhone.Trim();
            }

            // =====================================================
            // PAYMENT METHOD
            // =====================================================

            string paymentMethod =
                string.IsNullOrWhiteSpace(
                    booking.PaymentMethod)
                    ? "Pay at Venue"
                    : booking.PaymentMethod.Trim();

            if (LblPaymentMethod != null)
            {
                LblPaymentMethod.Text =
                    paymentMethod;
            }

            // =====================================================
            // PAYMENT STATUS
            // =====================================================

            string paymentStatus =
                string.IsNullOrWhiteSpace(
                    booking.PaymentStatus)
                    ? "Pending"
                    : booking.PaymentStatus.Trim();

            if (LblPaymentStatus != null)
            {
                LblPaymentStatus.Text =
                    paymentStatus;

                SetPaymentStatusColor(
                    paymentStatus);
            }

            // =====================================================
            // TOTAL
            // =====================================================

            if (LblTotalAmount != null)
            {
                LblTotalAmount.Text =
                    $"Rs. {booking.TotalAmount:N0}";
            }

            // =====================================================
            // BOOKING STATUS
            // =====================================================

            string bookingStatus =
                string.IsNullOrWhiteSpace(
                    booking.BookingStatus)
                    ? "Confirmed"
                    : booking.BookingStatus.Trim();

            if (LblBookingStatus != null)
            {
                LblBookingStatus.Text =
                    bookingStatus;

                SetBookingStatusColor(
                    bookingStatus);
            }
        }

        // =========================================================
        // PAYMENT STATUS COLOR
        // =========================================================

        private void SetPaymentStatusColor(
            string status)
        {
            if (LblPaymentStatus == null)
                return;

            if (string.Equals(
                    status,
                    "Paid",
                    StringComparison.OrdinalIgnoreCase))
            {
                LblPaymentStatus.TextColor =
                    Color.FromArgb("#03DAC6");
            }
            else if (string.Equals(
                         status,
                         "Pending",
                         StringComparison.OrdinalIgnoreCase))
            {
                LblPaymentStatus.TextColor =
                    Color.FromArgb("#FFB74D");
            }
            else if (string.Equals(
                         status,
                         "Failed",
                         StringComparison.OrdinalIgnoreCase))
            {
                LblPaymentStatus.TextColor =
                    Color.FromArgb("#FF5252");
            }
            else
            {
                LblPaymentStatus.TextColor =
                    Colors.White;
            }
        }

        // =========================================================
        // BOOKING STATUS COLOR
        // =========================================================

        private void SetBookingStatusColor(
            string status)
        {
            if (LblBookingStatus == null)
                return;

            if (string.Equals(
                    status,
                    "Confirmed",
                    StringComparison.OrdinalIgnoreCase))
            {
                LblBookingStatus.TextColor =
                    Color.FromArgb("#03DAC6");
            }
            else if (string.Equals(
                         status,
                         "Cancelled",
                         StringComparison.OrdinalIgnoreCase))
            {
                LblBookingStatus.TextColor =
                    Color.FromArgb("#FF5252");
            }
            else
            {
                LblBookingStatus.TextColor =
                    Colors.White;
            }
        }

        // =========================================================
        // RESET DISPLAY
        // =========================================================

        private void ResetDisplay()
        {
            if (LblBookingID != null)
                LblBookingID.Text = "#------";

            if (LblArenaName != null)
                LblArenaName.Text = "Arena";

            if (LblLocation != null)
                LblLocation.Text = "Location";

            if (LblSport != null)
                LblSport.Text = "Sport";

            if (LblCourt != null)
                LblCourt.Text = "Court";

            if (LblBookingDate != null)
                LblBookingDate.Text = "Date";

            if (LblBookingTime != null)
                LblBookingTime.Text = "Time";

            if (LblCustomerName != null)
                LblCustomerName.Text = "Customer";

            if (LblCustomerPhone != null)
                LblCustomerPhone.Text = "Phone";

            if (LblPaymentMethod != null)
                LblPaymentMethod.Text = "Pay at Venue";

            if (LblPaymentStatus != null)
            {
                LblPaymentStatus.Text = "Pending";

                LblPaymentStatus.TextColor =
                    Color.FromArgb("#FFB74D");
            }

            if (LblTotalAmount != null)
                LblTotalAmount.Text = "Rs. 0";

            if (LblBookingStatus != null)
            {
                LblBookingStatus.Text = "Confirmed";

                LblBookingStatus.TextColor =
                    Color.FromArgb("#03DAC6");
            }
        }

        // =========================================================
        // INTEGER QUERY VALUE
        // =========================================================

        private static int GetIntValue(
            IDictionary<string, object> query,
            string key)
        {
            if (query == null ||
                string.IsNullOrWhiteSpace(key) ||
                !query.TryGetValue(
                    key,
                    out object? value) ||
                value == null)
            {
                return 0;
            }

            if (value is int intValue)
                return intValue;

            if (value is long longValue &&
                longValue <= int.MaxValue &&
                longValue >= int.MinValue)
            {
                return (int)longValue;
            }

            if (value is short shortValue)
                return shortValue;

            if (value is byte byteValue)
                return byteValue;

            return int.TryParse(
                value.ToString(),
                out int parsedValue)
                    ? parsedValue
                    : 0;
        }

        // =========================================================
        // FORMAT TIME
        // =========================================================

        private static string FormatTime(
            TimeSpan time)
        {
            try
            {
                if (time < TimeSpan.Zero ||
                    time >= TimeSpan.FromDays(1))
                {
                    return time.ToString(@"hh\:mm");
                }

                return DateTime.Today
                    .Add(time)
                    .ToString("h:mm tt");
            }
            catch
            {
                return time.ToString(@"hh\:mm");
            }
        }

        // =========================================================
        // BACK / HOME
        // =========================================================

        private async void OnBackTapped(
     object? sender,
     EventArgs e)
        {
            try
            {
                if (Shell.Current == null)
                {
                    await ShowErrorAsync(
                        "Navigation Error",
                        "The application navigation system is unavailable.");

                    return;
                }

                await Shell.Current.GoToAsync(
                    "//CustomerHomePage");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Confirmation back navigation failed: {ex}");

                await ShowErrorAsync(
                    "Navigation Error",
                    "Unable to return to the customer home page.");
            }
        }

        // =========================================================
        // ERROR
        // =========================================================

        private async Task ShowErrorAsync(
            string title,
            string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title))
                    title = "Error";

                if (string.IsNullOrWhiteSpace(message))
                    message = "An unexpected error occurred.";

                await MainThread.InvokeOnMainThreadAsync(
                    async () =>
                    {
                        try
                        {
                            await DisplayAlertAsync(
                                title,
                                message,
                                "OK");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"DisplayAlert failed: {ex}");
                        }
                    });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"ShowErrorAsync failed: {ex}");
            }
        }
    }
}
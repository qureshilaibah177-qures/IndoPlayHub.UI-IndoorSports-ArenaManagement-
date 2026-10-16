
using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using IndoPlayHub.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace IndoPlayHub.UI
{
    public partial class CustomerBookingDetailsPage :
        ContentPage,
        IQueryAttributable
    {
        // =========================================================
        // DATABASE / SERVICES
        // =========================================================

        private readonly AppDbContext _dbContext;
        private readonly CustomerSessionService _sessionService;
        private readonly NotificationService _notificationService;


        // =========================================================
        // BOOKING
        // =========================================================

        private int _bookingID;

        private bool _queryReceived;

        private Booking? _booking;
        private Arena? _arena;
        private CourtOrMenuItem? _court;
        private Category? _category;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public CustomerBookingDetailsPage(
            AppDbContext dbContext,
            CustomerSessionService sessionService,
            NotificationService notificationService)
        {
            InitializeComponent();

            _dbContext = dbContext;
            _sessionService = sessionService;
            _notificationService = notificationService;
        }


        // =========================================================
        // QUERY
        // =========================================================

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            try
            {
                if (_queryReceived)
                    return;

                _queryReceived = true;

                _bookingID =
                    GetIntValue(
                        query,
                        "BookingID");

                _ = LoadBookingAsync();
            }
            catch (Exception ex)
            {
                _ = ShowErrorAsync(
                    "Booking",
                    $"Unable to open booking details.\n\n{ex.Message}");
            }
        }


        // =========================================================
        // LOAD BOOKING
        // =========================================================

        private async Task LoadBookingAsync()
        {
            try
            {
                if (_bookingID <= 0)
                {
                    await ShowErrorAsync(
                        "Booking",
                        "Invalid booking ID.");

                    return;
                }


                // =================================================
                // RESTORE CUSTOMER SESSION
                // =================================================

                Customer? customer =
                    await _sessionService
                        .RestoreSessionAsync();

                if (customer == null)
                {
                    await ShowErrorAsync(
                        "Login Required",
                        "Please login to continue.");

                    return;
                }


                // =================================================
                // LOAD CUSTOMER BOOKING
                // =================================================

                _booking =
                    await _dbContext.Bookings
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x =>
                                x.BookingID == _bookingID &&
                                x.CustomerPhone ==
                                customer.PhoneNumber);

                if (_booking == null)
                {
                    await ShowErrorAsync(
                        "Booking",
                        "Booking not found.");

                    return;
                }


                // =================================================
                // LOAD ARENA
                // =================================================

                _arena =
                    await _dbContext.Arenas
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x =>
                                x.ArenaID ==
                                _booking.ArenaID);


                // =================================================
                // LOAD COURT
                // =================================================

                _court =
                    await _dbContext.CourtsAndMenuItems
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x =>
                                x.ItemID ==
                                _booking.ItemID &&
                                x.ArenaID ==
                                _booking.ArenaID);


                // =================================================
                // LOAD CATEGORY / SPORT
                // =================================================

                if (_court != null)
                {
                    _category =
                        await _dbContext.Categories
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                x =>
                                    x.CategoryID ==
                                    _court.CategoryID);
                }


                // =================================================
                // DISPLAY
                // =================================================

                DisplayBooking();


                // =================================================
                // CANCELLATION UI
                // =================================================

                UpdateCancellationUI();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(
                    "Booking Error",
                    $"Unable to load booking details.\n\n{ex.Message}");
            }
        }


        // =========================================================
        // DISPLAY BOOKING
        // =========================================================

        private void DisplayBooking()
        {
            if (_booking == null)
                return;


            // =================================================
            // BOOKING ID
            // =================================================

            LblBookingID.Text =
                $"#{_booking.BookingID}";


            // =================================================
            // ARENA
            // =================================================

            LblArena.Text =
                string.IsNullOrWhiteSpace(
                    _arena?.ArenaName)
                    ? "Arena"
                    : _arena!.ArenaName;


            // =================================================
            // LOCATION
            // =================================================

            LblLocation.Text =
                string.IsNullOrWhiteSpace(
                    _arena?.Location)
                    ? "Location unavailable"
                    : _arena!.Location;


            // =================================================
            // SPORT
            // =================================================

            LblSport.Text =
                string.IsNullOrWhiteSpace(
                    _category?.CategoryName)
                    ? "Sport"
                    : _category!.CategoryName;


            // =================================================
            // COURT
            // =================================================

            LblCourt.Text =
                string.IsNullOrWhiteSpace(
                    _court?.ItemName)
                    ? "Court"
                    : _court!.ItemName;


            // =================================================
            // DATE
            // =================================================

            LblDate.Text =
                _booking.BookingDate
                    .ToString(
                        "dddd, dd MMMM yyyy");


            // =================================================
            // TIME
            // =================================================

            LblTime.Text =
                $"{FormatTime(_booking.StartTime)} - " +
                $"{FormatTime(_booking.EndTime)}";


            // =================================================
            // TOTAL
            // =================================================

            LblTotal.Text =
                $"Rs. {_booking.TotalAmount:N0}";


            // =================================================
            // PAYMENT METHOD
            // =================================================

            LblPaymentMethod.Text =
                string.IsNullOrWhiteSpace(
                    _booking.PaymentMethod)
                    ? "Pay at Venue"
                    : _booking.PaymentMethod;


            // =================================================
            // PAYMENT STATUS
            // =================================================

            LblPaymentStatus.Text =
                string.IsNullOrWhiteSpace(
                    _booking.PaymentStatus)
                    ? "Pending"
                    : _booking.PaymentStatus;


            // =================================================
            // BOOKING STATUS
            // =================================================

            LblBookingStatus.Text =
                string.IsNullOrWhiteSpace(
                    _booking.BookingStatus)
                    ? "Confirmed"
                    : _booking.BookingStatus;


            // =================================================
            // REFUND STATUS
            // =================================================

            LblRefundStatus.Text =
                string.IsNullOrWhiteSpace(
                    _booking.RefundStatus)
                    ? "Not Required"
                    : _booking.RefundStatus;


            // =================================================
            // CANCELLATION REASON
            // =================================================

            LblCancellationReason.Text =
                string.IsNullOrWhiteSpace(
                    _booking.CancellationReason)
                    ? "—"
                    : _booking.CancellationReason;
        }


        // =========================================================
        // CANCELLATION POLICY
        // =========================================================

        private void UpdateCancellationUI()
        {
            if (_booking == null ||
                _arena == null)
                return;


            bool isCancelled =
                string.Equals(
                    _booking.BookingStatus,
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase);


            DateTime bookingStart =
                _booking.BookingDate
                    .ToDateTime(
                        TimeOnly.FromTimeSpan(
                            _booking.StartTime));


            TimeSpan remaining =
                bookingStart -
                DateTime.Now;


            int cancellationHours =
                _arena.CancellationHours;


            bool eligible =
                !isCancelled &&
                bookingStart > DateTime.Now &&
                remaining.TotalHours >=
                cancellationHours;


            CancelBookingButton.IsVisible =
                eligible;


            PolicyLabel.Text =
                $"Free cancellation up to {cancellationHours} " +
                $"hours before your booking.";


            if (isCancelled)
            {
                PolicyLabel.Text =
                    "This booking has already been cancelled.";
            }
            else if (!eligible)
            {
                PolicyLabel.Text =
                    $"Cancellation is unavailable within " +
                    $"{cancellationHours} hours of the booking.";
            }
        }


        // =========================================================
        // CANCEL BOOKING
        // =========================================================

        private async void OnCancelBookingClicked(
            object? sender,
            EventArgs e)
        {
            if (_booking == null ||
                _arena == null)
                return;


            // =================================================
            // CHECK CANCELLATION WINDOW
            // =================================================

            DateTime bookingStart =
                _booking.BookingDate
                    .ToDateTime(
                        TimeOnly.FromTimeSpan(
                            _booking.StartTime));


            TimeSpan remaining =
                bookingStart -
                DateTime.Now;


            if (bookingStart <= DateTime.Now ||
                remaining.TotalHours <
                _arena.CancellationHours)
            {
                await ShowErrorAsync(
                    "Cancellation Unavailable",
                    $"This booking can only be cancelled " +
                    $"{_arena.CancellationHours} hours or more " +
                    "before the booking.");

                UpdateCancellationUI();

                return;
            }


            // =================================================
            // GET REASON
            // =================================================

            string? reason =
                await DisplayPromptAsync(
                    "Cancellation Reason",
                    "Please enter a reason for cancelling.",
                    "Cancel Booking",
                    "Keep Booking",
                    "Reason",
                    maxLength: 500);


            if (reason == null)
                return;


            if (string.IsNullOrWhiteSpace(reason))
            {
                await ShowErrorAsync(
                    "Cancellation Reason",
                    "Please provide a cancellation reason.");

                return;
            }


            // =================================================
            // CONFIRM
            // =================================================

            bool confirm =
                await DisplayAlertAsync(
                    "Cancel Booking",
                    "Are you sure you want to cancel this booking?",
                    "Yes, Cancel",
                    "No");


            if (!confirm)
                return;


            try
            {
                // =================================================
                // RESTORE CURRENT CUSTOMER
                // =================================================

                Customer? customer =
                    await _sessionService
                        .RestoreSessionAsync();

                if (customer == null)
                {
                    await ShowErrorAsync(
                        "Login Required",
                        "Please login to continue.");

                    return;
                }


                // =================================================
                // LOAD TRACKED BOOKING
                // =================================================

                Booking? booking =
                    await _dbContext.Bookings
                        .FirstOrDefaultAsync(
                            x =>
                                x.BookingID ==
                                _bookingID &&
                                x.CustomerPhone ==
                                customer.PhoneNumber);


                if (booking == null)
                {
                    await ShowErrorAsync(
                        "Booking",
                        "The booking could not be found.");

                    return;
                }


                // =================================================
                // RE-CHECK STATUS
                // =================================================

                if (string.Equals(
                    booking.BookingStatus,
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase))
                {
                    await ShowErrorAsync(
                        "Booking",
                        "This booking has already been cancelled.");

                    await LoadBookingAsync();

                    return;
                }


                // =================================================
                // RE-CHECK CANCELLATION WINDOW
                // =================================================

                DateTime finalBookingStart =
                    booking.BookingDate
                        .ToDateTime(
                            TimeOnly.FromTimeSpan(
                                booking.StartTime));


                TimeSpan finalRemaining =
                    finalBookingStart -
                    DateTime.Now;


                if (finalBookingStart <= DateTime.Now ||
                    finalRemaining.TotalHours <
                    _arena.CancellationHours)
                {
                    await ShowErrorAsync(
                        "Cancellation Unavailable",
                        $"This booking can only be cancelled " +
                        $"{_arena.CancellationHours} hours or more " +
                        "before the booking.");

                    await LoadBookingAsync();

                    return;
                }


                // =================================================
                // UPDATE BOOKING STATUS
                // =================================================

                booking.BookingStatus =
                    "Cancelled";

                booking.CancellationReason =
                    reason.Trim();

                booking.CancelledAt =
                    DateTime.Now;


                // =================================================
                // REFUND POLICY
                // =================================================

                if (string.Equals(
                    booking.PaymentStatus,
                    "Paid",
                    StringComparison.OrdinalIgnoreCase))
                {
                    booking.RefundStatus =
                        "Pending";
                }
                else
                {
                    booking.RefundStatus =
                        "Not Required";
                }


                // =================================================
                // SAVE CANCELLATION
                // =================================================

                await _dbContext.SaveChangesAsync();


                // =================================================
                // PHASE 16 — CANCELLATION NOTIFICATION
                // =================================================

                string arenaName =
                    string.IsNullOrWhiteSpace(
                        _arena.ArenaName)
                        ? "Arena"
                        : _arena.ArenaName;


                string sportName =
                    string.IsNullOrWhiteSpace(
                        _category?.CategoryName)
                        ? "Sport"
                        : _category!.CategoryName;


                string courtName =
                    string.IsNullOrWhiteSpace(
                        _court?.ItemName)
                        ? "Court"
                        : _court!.ItemName;


                string formattedDate =
                    booking.BookingDate.ToString(
                        "dddd, dd MMMM yyyy");


                string formattedTime =
                    $"{FormatTime(booking.StartTime)} - " +
                    $"{FormatTime(booking.EndTime)}";


                string notificationTitle =
                    "Booking Cancelled";


                string notificationMessage =
                    $"Your {sportName} booking at {arenaName} " +
                    $"has been cancelled.\n\n" +
                    $"Court: {courtName}\n" +
                    $"Date: {formattedDate}\n" +
                    $"Time: {formattedTime}\n" +
                    $"Booking ID: #{booking.BookingID}\n" +
                    $"Reason: {booking.CancellationReason}";


                try
                {
                    await _notificationService
                        .CreateNotificationAsync(
                            customer.CustomerID,
                            "Cancellation",
                            notificationTitle,
                            notificationMessage);
                }
                catch
                {
                    // The booking cancellation has already been
                    // successfully saved. Notification failure
                    // must not undo the cancellation.
                }


                // =================================================
                // SUCCESS MESSAGE
                // =================================================

                await DisplayAlertAsync(
                    "Booking Cancelled",
                    booking.RefundStatus ==
                    "Pending"
                        ? "Your booking has been cancelled. Your refund is pending."
                        : "Your booking has been cancelled successfully.",
                    "OK");


                // =================================================
                // REFRESH PAGE
                // =================================================

                await LoadBookingAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(
                    "Cancellation Error",
                    $"Unable to cancel this booking.\n\n{ex.Message}");
            }
        }


        // =========================================================
        // INTEGER QUERY VALUE
        // =========================================================

        private static int GetIntValue(
            IDictionary<string, object> query,
            string key)
        {
            if (!query.TryGetValue(
                key,
                out object? value))
            {
                return 0;
            }


            if (value is int intValue)
                return intValue;


            return int.TryParse(
                value?.ToString(),
                out int parsed)
                ? parsed
                : 0;
        }


        // =========================================================
        // FORMAT TIME
        // =========================================================

        private static string FormatTime(
            TimeSpan time)
        {
            return DateTime.Today
                .Add(time)
                .ToString("h:mm tt");
        }


        // =========================================================
        // BACK
        // =========================================================

        private async void OnBackTapped(
            object? sender,
            TappedEventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(
                    "Navigation Error",
                    $"Unable to go back.\n\n{ex.Message}");
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
                await MainThread.InvokeOnMainThreadAsync(
                    async () =>
                    {
                        await DisplayAlertAsync(
                            title,
                            message,
                            "OK");
                    });
            }
            catch
            {
                // Prevent secondary UI exceptions.
            }
        }
    }
}
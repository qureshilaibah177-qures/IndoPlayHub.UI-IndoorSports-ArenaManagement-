
using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using IndoPlayHub.UI.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace IndoPlayHub.UI
{
    public partial class CustomerMyBookingsPage : ContentPage
    {
        // =========================================================
        // DATABASE / SESSION
        // =========================================================

        private readonly AppDbContext _dbContext;
        private readonly CustomerSessionService _sessionService;


        // =========================================================
        // CUSTOMER
        // =========================================================

        private Customer? _customer;


        // =========================================================
        // BOOKINGS
        // =========================================================

        private readonly ObservableCollection<CustomerBookingDisplayItem>
            _bookings = new();

        public ObservableCollection<CustomerBookingDisplayItem> Bookings =>
            _bookings;


        // =========================================================
        // STATE
        // =========================================================

        private bool _isLoading;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public CustomerMyBookingsPage(
            AppDbContext dbContext,
            CustomerSessionService sessionService)
        {
            InitializeComponent();

            _dbContext = dbContext;
            _sessionService = sessionService;

            BindingContext = this;
        }


        // =========================================================
        // PAGE APPEARING
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_isLoading)
                return;

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
                // =================================================
                // RESTORE CUSTOMER SESSION
                // =================================================

                _customer =
                    await _sessionService
                        .RestoreSessionAsync();


                // =================================================
                // SESSION EXPIRED
                // =================================================

                if (_customer == null)
                {
                    _bookings.Clear();

                    UpdateEmptyStates();

                    await DisplayAlertAsync(
                        "Login Required",
                        "Please login to view your bookings.",
                        "OK");

                    await Shell.Current.GoToAsync(
                        "CustomerLoginPage");

                    return;
                }


                // =================================================
                // VALIDATE CUSTOMER PHONE
                // =================================================

                if (string.IsNullOrWhiteSpace(
                    _customer.PhoneNumber))
                {
                    _bookings.Clear();

                    UpdateEmptyStates();

                    await DisplayAlertAsync(
                        "Customer Account",
                        "Your customer account does not contain a valid phone number.",
                        "OK");

                    return;
                }


                // =================================================
                // LOAD BOOKINGS
                // =================================================

                var bookings =
                    await (
                        from booking in _dbContext.Bookings.AsNoTracking()

                        join arena in
                            _dbContext.Arenas.AsNoTracking()
                            on booking.ArenaID
                            equals arena.ArenaID

                        join court in
                            _dbContext.CourtsAndMenuItems.AsNoTracking()
                            on new
                            {
                                booking.ArenaID,
                                booking.ItemID
                            }
                            equals new
                            {
                                court.ArenaID,
                                court.ItemID
                            }

                        join category in
                            _dbContext.Categories.AsNoTracking()
                            on court.CategoryID
                            equals category.CategoryID

                        where
                            booking.CustomerPhone ==
                            _customer.PhoneNumber

                        orderby
                            booking.BookingDate descending,
                            booking.StartTime descending

                        select new
                        {
                            Booking = booking,
                            Arena = arena,
                            Court = court,
                            Category = category
                        })
                        .ToListAsync();


                // =================================================
                // CLEAR OLD DATA
                // =================================================

                _bookings.Clear();


                // =================================================
                // CURRENT DATE / TIME
                // =================================================

                DateTime now = DateTime.Now;


                // =================================================
                // BUILD DISPLAY MODELS
                // =================================================

                foreach (var item in bookings)
                {
                    DateTime bookingStart =
                        item.Booking.BookingDate
                            .ToDateTime(
                                TimeOnly.FromTimeSpan(
                                    item.Booking.StartTime));


                    DateTime bookingEnd =
                        item.Booking.BookingDate
                            .ToDateTime(
                                TimeOnly.FromTimeSpan(
                                    item.Booking.EndTime));


                    // =================================================
                    // STATUS
                    // =================================================

                    bool isCancelled =
                        string.Equals(
                            item.Booking.BookingStatus,
                            "Cancelled",
                            StringComparison.OrdinalIgnoreCase);


                    bool isCompleted =
                        !isCancelled &&
                        bookingEnd <= now;


                    bool isUpcoming =
                        !isCancelled &&
                        bookingEnd > now;


                    // =================================================
                    // DISPLAY MODEL
                    // =================================================

                    _bookings.Add(
                        new CustomerBookingDisplayItem
                        {
                            // -----------------------------------------
                            // IDENTIFIERS
                            // -----------------------------------------

                            BookingID =
                                item.Booking.BookingID,

                            ArenaID =
                                item.Booking.ArenaID,

                            ItemID =
                                item.Booking.ItemID,


                            // -----------------------------------------
                            // ARENA / SPORT / COURT
                            // -----------------------------------------

                            ArenaName =
                                string.IsNullOrWhiteSpace(
                                    item.Arena.ArenaName)
                                    ? "Arena"
                                    : item.Arena.ArenaName,

                            SportName =
                                string.IsNullOrWhiteSpace(
                                    item.Category.CategoryName)
                                    ? "Sport"
                                    : item.Category.CategoryName,

                            CourtName =
                                string.IsNullOrWhiteSpace(
                                    item.Court.ItemName)
                                    ? "Court"
                                    : item.Court.ItemName,


                            // -----------------------------------------
                            // DATE
                            // -----------------------------------------

                            BookingDateText =
                                item.Booking.BookingDate
                                    .ToString(
                                        "dd MMM yyyy"),


                            // -----------------------------------------
                            // TIME
                            // -----------------------------------------

                            BookingTimeText =
                                $"{FormatTime(item.Booking.StartTime)} - " +
                                $"{FormatTime(item.Booking.EndTime)}",


                            // -----------------------------------------
                            // PRICE
                            // -----------------------------------------

                            TotalAmount =
                                item.Booking.TotalAmount,


                            // -----------------------------------------
                            // BOOKING STATUS
                            // -----------------------------------------

                            BookingStatus =
                                string.IsNullOrWhiteSpace(
                                    item.Booking.BookingStatus)
                                    ? "Confirmed"
                                    : item.Booking.BookingStatus,


                            // -----------------------------------------
                            // PAYMENT
                            // -----------------------------------------

                            PaymentStatus =
                                string.IsNullOrWhiteSpace(
                                    item.Booking.PaymentStatus)
                                    ? "Pending"
                                    : item.Booking.PaymentStatus,

                            PaymentMethod =
                                string.IsNullOrWhiteSpace(
                                    item.Booking.PaymentMethod)
                                    ? "Pay at Venue"
                                    : item.Booking.PaymentMethod,


                            // -----------------------------------------
                            // CANCELLATION
                            // -----------------------------------------

                            CancellationReason =
                                item.Booking.CancellationReason
                                ?? string.Empty,


                            // -----------------------------------------
                            // REFUND
                            // -----------------------------------------

                            RefundStatus =
                                string.IsNullOrWhiteSpace(
                                    item.Booking.RefundStatus)
                                    ? "Not Required"
                                    : item.Booking.RefundStatus,


                            // -----------------------------------------
                            // CATEGORY FLAGS
                            // -----------------------------------------

                            IsUpcoming =
                                isUpcoming,

                            IsCompleted =
                                isCompleted,

                            IsCancelled =
                                isCancelled
                        });
                }


                // =================================================
                // UPDATE EMPTY / SECTION STATES
                // =================================================

                UpdateEmptyStates();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Bookings Error",
                    $"Unable to load your bookings.\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                _isLoading = false;
            }
        }


        // =========================================================
        // VIEW DETAILS
        // =========================================================

        private async void OnViewDetailsClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                if (sender is not Button button)
                    return;

                if (button.CommandParameter
                    is not CustomerBookingDisplayItem booking)
                {
                    return;
                }


                if (booking.BookingID <= 0)
                {
                    await DisplayAlertAsync(
                        "Booking",
                        "Invalid booking selected.",
                        "OK");

                    return;
                }


                // =================================================
                // OPEN AUTHORITATIVE BOOKING DETAILS
                // =================================================

                await Shell.Current.GoToAsync(
                    "CustomerBookingDetailsPage",
                    new Dictionary<string, object>
                    {
                        ["BookingID"] =
                            booking.BookingID
                    });
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Booking Details",
                    $"Unable to open booking details.\n\n{ex.Message}",
                    "OK");
            }
        }


        // =========================================================
        // EMPTY / SECTION STATES
        // =========================================================

        private void UpdateEmptyStates()
        {
            bool hasUpcoming =
                _bookings.Any(
                    x => x.IsUpcoming);


            bool hasCompleted =
                _bookings.Any(
                    x => x.IsCompleted);


            bool hasCancelled =
                _bookings.Any(
                    x => x.IsCancelled);


            // =====================================================
            // SECTIONS
            // =====================================================

            UpcomingSection.IsVisible =
                hasUpcoming;

            CompletedSection.IsVisible =
                hasCompleted;

            CancelledSection.IsVisible =
                hasCancelled;


            // =====================================================
            // GLOBAL EMPTY STATE
            // =====================================================

            EmptyState.IsVisible =
                !hasUpcoming &&
                !hasCompleted &&
                !hasCancelled;
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
                await DisplayAlertAsync(
                    "Navigation Error",
                    $"Unable to go back.\n\n{ex.Message}",
                    "OK");
            }
        }
    }
}


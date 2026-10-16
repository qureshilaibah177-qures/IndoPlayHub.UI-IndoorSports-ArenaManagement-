
using System.ComponentModel;
using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using Microsoft.EntityFrameworkCore;

namespace IndoPlayHub.UI
{
    public partial class AddBookingPage : ContentPage
    {
        private readonly AppDbContext _dbContext;
        private readonly int _currentArenaId;

        private List<CourtOrMenuItem> _courts = new();

        private bool _isSaving;

        public AddBookingPage(
            AppDbContext dbContext,
            int arenaId)
        {
            InitializeComponent();

            _dbContext = dbContext;
            _currentArenaId = arenaId;

            // =====================================================
            // DEFAULT VALUES
            // =====================================================

            DpBookingDate.MinimumDate = DateTime.Today;
            DpBookingDate.Date = DateTime.Today;

            TpStartTime.Time = new TimeSpan(8, 0, 0);
            TpEndTime.Time = new TimeSpan(9, 0, 0);

            PickerPaymentStatus.SelectedIndex = 0;
            PickerBookingStatus.SelectedIndex = 0;
        }

        // =========================================================
        // PAGE APPEARS
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await LoadCourtsAsync();
        }

        // =========================================================
        // LOAD COURTS
        // =========================================================

        private async Task LoadCourtsAsync()
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

                _courts = await _dbContext.CourtsAndMenuItems
                    .AsNoTracking()
                    .Include(x => x.Category)
                    .Where(x =>
                        x.ArenaID == _currentArenaId &&
                        x.IsAvailable)
                    .OrderBy(x => x.Category.CategoryName)
                    .ThenBy(x => x.ItemName)
                    .ToListAsync();

                PickerCourt.ItemsSource = _courts;

                PickerCourt.ItemDisplayBinding =
                    new Binding(
                        nameof(CourtOrMenuItem.ItemName));

                if (_courts.Count > 0)
                {
                    PickerCourt.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Booking Error",
                    $"Could not load courts:\n\n{ex.Message}",
                    "OK");
            }
        }

        // =========================================================
        // COURT CHANGED
        // =========================================================

        private void OnCourtChanged(
            object? sender,
            EventArgs e)
        {
            UpdateTotalAmount();
        }

        // =========================================================
        // TIME CHANGED
        // =========================================================

        private void OnTimeChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TimePicker.Time))
            {
                UpdateTotalAmount();
            }
        }

        // =========================================================
        // CALCULATE TOTAL
        // =========================================================

        private void UpdateTotalAmount()
        {
            if (PickerCourt.SelectedItem is not CourtOrMenuItem selectedCourt)
            {
                LblSelectedRate.Text = "Rate: Rs. 0 / hour";
                LblTotalAmount.Text = "Total: Rs. 0";
                return;
            }

            LblSelectedRate.Text =
                $"Rate: Rs. {selectedCourt.PricePerHour:N0} / hour";

            TimeSpan startTime =
                TpStartTime.Time ?? TimeSpan.Zero;

            TimeSpan endTime =
                TpEndTime.Time ?? TimeSpan.Zero;

            TimeSpan duration =
                endTime - startTime;

            if (duration.TotalMinutes <= 0)
            {
                LblTotalAmount.Text = "Total: Rs. 0";
                return;
            }

            decimal total =
                selectedCourt.PricePerHour *
                (decimal)duration.TotalHours;

            LblTotalAmount.Text =
                $"Total: Rs. {total:N0}";
        }

        // =========================================================
        // SAVE BOOKING
        // =========================================================

        private async void OnSaveBookingClicked(
            object? sender,
            EventArgs e)
        {
            if (_isSaving)
                return;

            _isSaving = true;

            try
            {
                // =================================================
                // CUSTOMER VALIDATION
                // =================================================

                string customerName =
                    TxtCustomerName.Text?.Trim()
                    ?? string.Empty;

                string customerPhone =
                    TxtCustomerPhone.Text?.Trim()
                    ?? string.Empty;

                if (string.IsNullOrWhiteSpace(customerName))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter the customer name.",
                        "OK");

                    return;
                }

                if (string.IsNullOrWhiteSpace(customerPhone))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter the customer phone number.",
                        "OK");

                    return;
                }

                // =================================================
                // COURT VALIDATION
                // =================================================

                if (PickerCourt.SelectedItem
                    is not CourtOrMenuItem selectedCourt)
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please select a court.",
                        "OK");

                    return;
                }

                // =================================================
                // TIME VALIDATION
                // =================================================

                TimeSpan startTime =
                    TpStartTime.Time ?? TimeSpan.Zero;

                TimeSpan endTime =
                    TpEndTime.Time ?? TimeSpan.Zero;

                if (endTime <= startTime)
                {
                    await DisplayAlertAsync(
                        "Invalid Time",
                        "End time must be later than start time.",
                        "OK");

                    return;
                }

                // =================================================
                // ARENA VALIDATION
                // =================================================

                var arena = await _dbContext.Arenas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a =>
                        a.ArenaID == _currentArenaId);

                if (arena == null)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "Arena could not be found.",
                        "OK");

                    return;
                }

                // =================================================
                // BUSINESS HOURS VALIDATION
                // =================================================

                if (startTime < arena.OpeningTime ||
                    endTime > arena.ClosingTime)
                {
                    await DisplayAlertAsync(
                        "Outside Business Hours",
                        $"This arena is open from " +
                        $"{arena.OpeningTime:hh\\:mm} to " +
                        $"{arena.ClosingTime:hh\\:mm}.",
                        "OK");

                    return;
                }

                // =================================================
                // DATE VALIDATION
                // =================================================

                // MAUI DatePicker uses DateTime.
                // Booking.BookingDate uses DateOnly.
                // Convert once here so both admin and customer
                // sides use the same Booking model.

                DateOnly bookingDate =
    DateOnly.FromDateTime(
        DpBookingDate.Date ?? DateTime.Today);

                DateOnly today =
                    DateOnly.FromDateTime(
                        DateTime.Today);

                if (bookingDate < today)
                {
                    await DisplayAlertAsync(
                        "Invalid Date",
                        "Booking date cannot be in the past.",
                        "OK");

                    return;
                }

                // =================================================
                // CHECK OVERLAPPING BOOKINGS
                // =================================================

                bool alreadyBooked =
                    await _dbContext.Bookings
                        .AsNoTracking()
                        .AnyAsync(b =>
                            b.ArenaID == _currentArenaId &&
                            b.ItemID == selectedCourt.ItemID &&
                            b.BookingDate == bookingDate &&
                            b.BookingStatus != "Cancelled" &&
                            startTime < b.EndTime &&
                            endTime > b.StartTime);

                if (alreadyBooked)
                {
                    await DisplayAlertAsync(
                        "Court Already Booked",
                        "This court is already booked during the selected time.",
                        "OK");

                    return;
                }

                // =================================================
                // CALCULATE TOTAL
                // =================================================

                decimal totalAmount =
                    selectedCourt.PricePerHour *
                    (decimal)(endTime - startTime).TotalHours;

                // =================================================
                // CREATE BOOKING
                // =================================================

                var booking = new Booking
                {
                    ArenaID = _currentArenaId,

                    ItemID = selectedCourt.ItemID,

                    CustomerName = customerName,

                    CustomerPhone = customerPhone,

                    BookingDate = bookingDate,

                    StartTime = startTime,

                    EndTime = endTime,

                    TotalAmount = totalAmount,

                    BookingStatus =
                        PickerBookingStatus.SelectedItem
                            ?.ToString()
                        ?? "Confirmed",

                    PaymentStatus =
                        PickerPaymentStatus.SelectedItem
                            ?.ToString()
                        ?? "Pending"
                };

                _dbContext.Bookings.Add(booking);

                await _dbContext.SaveChangesAsync();

                // =================================================
                // SUCCESS
                // =================================================

                await DisplayAlertAsync(
                    "Booking Saved",
                    $"Booking created successfully.\n\n" +
                    $"Customer: {customerName}\n" +
                    $"Court: {selectedCourt.ItemName}\n" +
                    $"Total: Rs. {totalAmount:N0}",
                    "OK");

                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Booking Error",
                    $"Could not save booking:\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                _isSaving = false;
            }
        }

        // =========================================================
        // CANCEL
        // =========================================================

        private async void OnCancelClicked(
            object? sender,
            EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}

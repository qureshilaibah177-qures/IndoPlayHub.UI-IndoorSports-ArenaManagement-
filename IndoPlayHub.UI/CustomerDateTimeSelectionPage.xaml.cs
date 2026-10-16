
using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using Microsoft.EntityFrameworkCore;

namespace IndoPlayHub.UI
{
    public partial class CustomerDateTimeSelectionPage :
        ContentPage,
        IQueryAttributable
    {
        // =========================================================
        // DATABASE
        // =========================================================

        private readonly AppDbContext _dbContext;


        // =========================================================
        // SELECTION
        // =========================================================

        private int _arenaID;
        private int _categoryID;
        private int _itemID;


        // =========================================================
        // SELECTED DATA
        // =========================================================

        private CourtOrMenuItem? _selectedCourt;
        private Arena? _selectedArena;


        // =========================================================
        // STATE
        // =========================================================

        private bool _isLoading;
        private bool _queryReceived;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public CustomerDateTimeSelectionPage(
            AppDbContext dbContext)
        {
            InitializeComponent();

            _dbContext = dbContext;

            DateTime today = DateTime.Today;

            BookingDatePicker.MinimumDate = today;
            BookingDatePicker.Date = today;

            UpdateSelectedDateText();
        }


        // =========================================================
        // QUERY PARAMETERS
        // =========================================================

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            try
            {
                _arenaID =
                    GetIntQueryValue(
                        query,
                        "ArenaID");

                _categoryID =
                    GetIntQueryValue(
                        query,
                        "CategoryID");

                _itemID =
                    GetIntQueryValue(
                        query,
                        "ItemID");


                if (_queryReceived)
                    return;

                _queryReceived = true;

                _ = LoadSelectionAsync();
            }
            catch (Exception ex)
            {
                _ = ShowErrorAsync(
                    "Loading Error",
                    $"Unable to open date and time selection.\n\n{ex.Message}");
            }
        }


        // =========================================================
        // QUERY VALUE HELPER
        // =========================================================

        private static int GetIntQueryValue(
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
                out int parsedValue)
                    ? parsedValue
                    : 0;
        }


        // =========================================================
        // LOAD ARENA + COURT
        // =========================================================

        private async Task LoadSelectionAsync()
        {
            if (_isLoading)
                return;

            _isLoading = true;

            try
            {
                ShowLoadingState();


                // =================================================
                // VALIDATION
                // =================================================

                if (_arenaID <= 0)
                {
                    await ShowErrorAsync(
                        "Arena",
                        "Invalid arena selected.");

                    return;
                }

                if (_itemID <= 0)
                {
                    await ShowErrorAsync(
                        "Court",
                        "Invalid court selected.");

                    return;
                }


                // =================================================
                // LOAD ARENA
                // =================================================

                _selectedArena =
                    await _dbContext.Arenas
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x =>
                                x.ArenaID == _arenaID);

                if (_selectedArena == null)
                {
                    await ShowErrorAsync(
                        "Arena",
                        "The selected arena could not be found.");

                    return;
                }


                // =================================================
                // LOAD COURT
                // =================================================

                _selectedCourt =
                    await _dbContext.CourtsAndMenuItems
                        .AsNoTracking()
                        .Include(x => x.Category)
                        .FirstOrDefaultAsync(
                            x =>
                                x.ItemID == _itemID &&
                                x.ArenaID == _arenaID);

                if (_selectedCourt == null)
                {
                    await ShowErrorAsync(
                        "Court",
                        "The selected court could not be found.");

                    return;
                }


                // =================================================
                // VERIFY CATEGORY
                // =================================================

                if (_categoryID > 0 &&
                    _selectedCourt.CategoryID != _categoryID)
                {
                    await ShowErrorAsync(
                        "Court",
                        "The selected court does not belong to the selected sport.");

                    return;
                }


                // =================================================
                // DISPLAY ARENA
                // =================================================

                LblArenaName.Text =
                    string.IsNullOrWhiteSpace(
                        _selectedArena.ArenaName)
                        ? "Arena"
                        : _selectedArena.ArenaName;

                LblArenaLocation.Text =
                    string.IsNullOrWhiteSpace(
                        _selectedArena.Location)
                        ? "📍 Location unavailable"
                        : $"📍 {_selectedArena.Location}";


                // =================================================
                // DISPLAY COURT
                // =================================================

                LblCourtName.Text =
                    string.IsNullOrWhiteSpace(
                        _selectedCourt.ItemName)
                        ? "Court"
                        : _selectedCourt.ItemName;

                LblCourtPrice.Text =
                    $"Rs. {_selectedCourt.PricePerHour:N0} / hour";

                LblPageTitle.Text =
                    "Select Date & Time";


                // =================================================
                // LOAD AVAILABILITY
                // =================================================

                DateTime selectedDate =
                    BookingDatePicker.Date ?? DateTime.Today;

                await LoadTimeSlotsAsync(
                    selectedDate);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(
                    "Date & Time Error",
                    $"Unable to load date and time selection.\n\n{ex.Message}");
            }
            finally
            {
                HideLoadingState();

                _isLoading = false;
            }
        }


        // =========================================================
        // DATE SELECTED
        // =========================================================

        private async void OnBookingDateSelected(
            object? sender,
            DateChangedEventArgs e)
        {
            try
            {
                DateTime selectedDate =
                    e.NewDate ?? DateTime.Today;

                UpdateSelectedDateText();

                await LoadTimeSlotsAsync(
                    selectedDate);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(
                    "Date",
                    $"Unable to check availability for this date.\n\n{ex.Message}");
            }
        }


        // =========================================================
        // LOAD TIME SLOTS
        // =========================================================

        private async Task LoadTimeSlotsAsync(
            DateTime selectedDate)
        {
            if (_selectedArena == null ||
                _selectedCourt == null)
            {
                return;
            }

            try
            {
                ShowLoadingState();


                // =================================================
                // CONVERT DATETIME -> DATEONLY
                // =================================================

                DateOnly bookingDate =
                    DateOnly.FromDateTime(
                        selectedDate);

                DateOnly nextDate =
                    bookingDate.AddDays(1);


                // =================================================
                // DISPLAY DATE
                // =================================================

                UpdateSelectedDateText();


                // =================================================
                // ARENA HOURS
                // =================================================

                TimeSpan openingTime =
                    _selectedArena.OpeningTime;

                TimeSpan closingTime =
                    _selectedArena.ClosingTime;


                // =================================================
                // OVERNIGHT CHECK
                // =================================================

                bool closesNextDay =
                    closingTime <= openingTime;


                // =================================================
                // LOAD BOOKINGS
                // =================================================

                List<Booking> bookings =
                    await _dbContext.Bookings
                        .AsNoTracking()
                        .Where(
                            x =>
                                x.ArenaID == _arenaID &&
                                x.ItemID == _itemID &&
                                x.BookingDate == bookingDate)
                        .ToListAsync();


                // =================================================
                // CREATE SLOT LIST
                // =================================================

                List<CustomerDateTimeSelectionItem> slots =
                    new();


                // =================================================
                // NORMAL SAME-DAY ARENA
                // =================================================

                if (!closesNextDay)
                {
                    AddSlots(
                        slots,
                        openingTime,
                        closingTime,
                        bookingDate,
                        bookings);
                }
                else
                {
                    // =================================================
                    // PART 1 — OPENING TO MIDNIGHT
                    // =================================================

                    AddSlots(
                        slots,
                        openingTime,
                        TimeSpan.FromHours(24),
                        bookingDate,
                        bookings);


                    // =================================================
                    // PART 2 — MIDNIGHT TO CLOSING
                    // =================================================

                    List<Booking> nextDayBookings =
                        await _dbContext.Bookings
                            .AsNoTracking()
                            .Where(
                                x =>
                                    x.ArenaID == _arenaID &&
                                    x.ItemID == _itemID &&
                                    x.BookingDate == nextDate)
                            .ToListAsync();

                    AddSlots(
                        slots,
                        TimeSpan.Zero,
                        closingTime,
                        nextDate,
                        nextDayBookings);
                }


                // =================================================
                // DISPLAY
                // =================================================

                CvTimeSlots.ItemsSource =
                    slots;


                // =================================================
                // EMPTY STATE
                // =================================================

                if (slots.Count == 0)
                {
                    CvTimeSlots.IsVisible =
                        false;

                    EmptyStateLayout.IsVisible =
                        true;

                    LblEmptyMessage.Text =
                        "No hourly slots could be generated for this arena.";
                }
                else
                {
                    CvTimeSlots.IsVisible =
                        true;

                    EmptyStateLayout.IsVisible =
                        false;
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(
                    "Availability Error",
                    $"Unable to calculate available time slots.\n\n{ex.Message}");
            }
            finally
            {
                HideLoadingState();
            }
        }


        // =========================================================
        // ADD HOURLY SLOTS
        // =========================================================

        private void AddSlots(
            List<CustomerDateTimeSelectionItem> slots,
            TimeSpan start,
            TimeSpan end,
            DateOnly slotDate,
            List<Booking> bookings)
        {
            TimeSpan current = start;

            // Prevent invalid ranges.
            if (end <= start)
                return;

            while (
                current.Add(
                    TimeSpan.FromHours(1)) <= end)
            {
                TimeSpan slotEnd =
                    current.Add(
                        TimeSpan.FromHours(1));


                // =================================================
                // CHECK BOOKING
                // =================================================

                bool isBooked =
                    bookings.Any(
                        booking =>
                            BookingOverlapsSlot(
                                booking,
                                current,
                                slotEnd));


                // =================================================
                // CHECK COURT
                // =================================================

                bool isCourtAvailable =
                    _selectedCourt?.IsAvailable == true;


                // =================================================
                // ADD SLOT
                // =================================================

                slots.Add(
                    new CustomerDateTimeSelectionItem
                    {
                        BookingDate =
                            slotDate,

                        StartTime =
                            current,

                        EndTime =
                            slotEnd,

                        PricePerHour =
                            _selectedCourt?.PricePerHour ?? 0,

                        IsAvailable =
                            isCourtAvailable &&
                            !isBooked
                    });


                current = slotEnd;
            }
        }


        // =========================================================
        // BOOKING OVERLAP
        // =========================================================

        private static bool BookingOverlapsSlot(
            Booking booking,
            TimeSpan slotStart,
            TimeSpan slotEnd)
        {
            if (string.Equals(
                booking.BookingStatus,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return
                booking.StartTime < slotEnd &&
                booking.EndTime > slotStart;
        }


        // =========================================================
        // DATE DISPLAY
        // =========================================================

        private void UpdateSelectedDateText()
        {
            DateTime selectedDate =
                BookingDatePicker.Date ?? DateTime.Today;

            LblSelectedDate.Text =
                selectedDate.ToString(
                    "dddd, dd MMMM yyyy");
        }

       
// =========================================================
// TIME SLOT SELECTED
// =========================================================

private async void OnTimeSlotSelectedClicked(
    object? sender,
    EventArgs e)
        {
            try
            {
                if (sender is not Button button)
                    return;


                if (button.CommandParameter
                    is not CustomerDateTimeSelectionItem slot)
                {
                    await ShowErrorAsync(
                        "Time Slot",
                        "Unable to identify the selected time slot.");

                    return;
                }


                // =====================================================
                // AVAILABILITY CHECK
                // =====================================================

                if (!slot.IsAvailable)
                {
                    await DisplayAlertAsync(
                        "Time Unavailable",
                        "This time slot is no longer available.",
                        "OK");

                    return;
                }


                // =====================================================
                // VALIDATE SELECTION
                // =====================================================

                if (_arenaID <= 0 ||
                    _categoryID <= 0 ||
                    _itemID <= 0)
                {
                    await ShowErrorAsync(
                        "Booking",
                        "The selected arena, sport, or court information is missing.");

                    return;
                }


                if (slot.BookingDate == default)
                {
                    await ShowErrorAsync(
                        "Booking Date",
                        "The selected booking date is invalid.");

                    return;
                }


                if (slot.EndTime <= slot.StartTime)
                {
                    await ShowErrorAsync(
                        "Booking Time",
                        "The selected booking time is invalid.");

                    return;
                }


                // =====================================================
                // FINAL AVAILABILITY CHECK
                // =====================================================

                bool alreadyBooked =
                    await _dbContext.Bookings
                        .AsNoTracking()
                        .AnyAsync(
                            booking =>
                                booking.ArenaID == _arenaID &&
                                booking.ItemID == _itemID &&
                                booking.BookingDate == slot.BookingDate &&
                                booking.BookingStatus != "Cancelled" &&
                                booking.StartTime < slot.EndTime &&
                                booking.EndTime > slot.StartTime);

                if (alreadyBooked)
                {
                    await ShowErrorAsync(
                        "Time Unavailable",
                        "This time slot has just been booked. Please select another time.");

                    // Refresh the slots so the UI reflects the latest database state.
                    await LoadTimeSlotsAsync(
                        slot.BookingDate.ToDateTime(TimeOnly.MinValue));

                    return;
                }


                // =====================================================
                // NAVIGATE TO BOOKING SUMMARY
                // =====================================================

                await Shell.Current.GoToAsync(
                    "CustomerBookingSummaryPage",
                    new Dictionary<string, object>
                    {
                        ["ArenaID"] = _arenaID,

                        ["CategoryID"] = _categoryID,

                        ["ItemID"] = _itemID,

                        ["BookingDate"] = slot.BookingDate,

                        ["StartTime"] = slot.StartTime,

                        ["EndTime"] = slot.EndTime
                    });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(
                    "Booking Selection Error",
                    $"Unable to continue to booking summary.\n\n{ex.Message}");
            }
        }



        // =========================================================
        // LOADING
        // =========================================================

        private void ShowLoadingState()
        {
            LoadingLayout.IsVisible = true;

            LoadingIndicator.IsRunning = true;

            CvTimeSlots.IsVisible = false;

            EmptyStateLayout.IsVisible = false;
        }


        private void HideLoadingState()
        {
            LoadingIndicator.IsRunning = false;

            LoadingLayout.IsVisible = false;
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
    }
}


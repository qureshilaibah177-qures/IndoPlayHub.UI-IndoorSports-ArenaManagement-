using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using IndoPlayHub.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace IndoPlayHub.UI
{
    public partial class CustomerBookingSummaryPage :
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
        // SELECTION
        // =========================================================

        private int _arenaID;
        private int _categoryID;
        private int _itemID;


        // =========================================================
        // BOOKING VALUES
        // =========================================================

        private DateOnly _bookingDate;
        private TimeSpan _startTime;
        private TimeSpan _endTime;

        private decimal _courtPrice;
        private decimal _serviceFee;
        private decimal _totalAmount;


        // =========================================================
        // PAYMENT
        // =========================================================

        private const string PayAtVenue = "Pay at Venue";
        private const string OnlinePayment = "Online Payment";
        private const string Pending = "Pending";

        private string _paymentMethod = PayAtVenue;
        private string _paymentStatus = Pending;


        // =========================================================
        // STATE
        // =========================================================

        private bool _queryReceived;
        private bool _isLoading;
        private bool _isConfirming;
        private bool _pageInitialized;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public CustomerBookingSummaryPage(
            AppDbContext dbContext,
            CustomerSessionService sessionService,
            NotificationService notificationService)
        {
            InitializeComponent();

            _dbContext = dbContext
                ?? throw new ArgumentNullException(nameof(dbContext));

            _sessionService = sessionService
                ?? throw new ArgumentNullException(nameof(sessionService));

            _notificationService = notificationService
                ?? throw new ArgumentNullException(nameof(notificationService));

            _serviceFee = 0m;
            _paymentMethod = PayAtVenue;
            _paymentStatus = Pending;

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

                /*
                 * Query parameters are loaded through
                 * ApplyQueryAttributes().
                 *
                 * We intentionally do NOT reload the summary
                 * every time the page appears because doing so
                 * could reset the user's payment selection.
                 */
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"CustomerBookingSummaryPage OnAppearing error: {ex}");
            }
        }


        // =========================================================
        // QUERY PARAMETERS
        // =========================================================

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            try
            {
                if (query == null || query.Count == 0)
                {
                    _queryReceived = false;

                    _ = ShowErrorAsync(
                        "Booking Summary",
                        "No booking information was received.");

                    return;
                }


                // =================================================
                // READ VALUES
                // =================================================

                int arenaID =
                    GetIntValue(query, "ArenaID");

                int categoryID =
                    GetIntValue(query, "CategoryID");

                int itemID =
                    GetIntValue(query, "ItemID");

                DateOnly bookingDate =
                    GetDateOnlyValue(query, "BookingDate");

                TimeSpan startTime =
                    GetTimeSpanValue(query, "StartTime");

                TimeSpan endTime =
                    GetTimeSpanValue(query, "EndTime");


                // =================================================
                // VALIDATE
                // =================================================

                if (arenaID <= 0)
                {
                    _queryReceived = false;

                    _ = ShowErrorAsync(
                        "Booking Summary",
                        "The selected arena is invalid.");

                    return;
                }

                if (itemID <= 0)
                {
                    _queryReceived = false;

                    _ = ShowErrorAsync(
                        "Booking Summary",
                        "The selected court is invalid.");

                    return;
                }

                if (bookingDate == default)
                {
                    _queryReceived = false;

                    _ = ShowErrorAsync(
                        "Booking Summary",
                        "The booking date is invalid.");

                    return;
                }

                if (endTime <= startTime)
                {
                    _queryReceived = false;

                    _ = ShowErrorAsync(
                        "Booking Summary",
                        "The selected booking time is invalid.");

                    return;
                }


                // =================================================
                // STORE
                // =================================================

                _arenaID = arenaID;
                _categoryID = categoryID;
                _itemID = itemID;

                _bookingDate = bookingDate;
                _startTime = startTime;
                _endTime = endTime;

                _queryReceived = true;


                // =================================================
                // LOAD SUMMARY
                // =================================================

                _ = LoadSummarySafelyAsync();
            }
            catch (Exception ex)
            {
                _queryReceived = false;

                System.Diagnostics.Debug.WriteLine(
                    $"ApplyQueryAttributes failed: {ex}");

                _ = ShowErrorAsync(
                    "Booking Summary",
                    "Unable to read the booking information.");
            }
        }


        // =========================================================
        // SAFE SUMMARY LOAD
        // =========================================================

        private async Task LoadSummarySafelyAsync()
        {
            try
            {
                await LoadSummaryAsync();
            }
            catch (OperationCanceledException)
            {
                // Safe cancellation.
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"LoadSummarySafelyAsync failed: {ex}");

                await ShowErrorAsync(
                    "Booking Summary",
                    "Unable to load the booking summary.");
            }
        }


        // =========================================================
        // LOAD SUMMARY
        // =========================================================

        private async Task LoadSummaryAsync()
        {
            if (_isLoading)
                return;

            if (!_queryReceived)
                return;

            _isLoading = true;

            try
            {
                SetLoadingState(true);


                // =================================================
                // VALIDATE IDS
                // =================================================

                if (_arenaID <= 0)
                {
                    await ShowErrorAsync(
                        "Booking",
                        "Invalid arena selected.");

                    return;
                }

                if (_itemID <= 0)
                {
                    await ShowErrorAsync(
                        "Booking",
                        "Invalid court selected.");

                    return;
                }


                // =================================================
                // VALIDATE DATE
                // =================================================

                if (_bookingDate == default)
                {
                    await ShowErrorAsync(
                        "Booking",
                        "Invalid booking date.");

                    return;
                }


                // =================================================
                // VALIDATE TIME
                // =================================================

                if (_endTime <= _startTime)
                {
                    await ShowErrorAsync(
                        "Booking",
                        "Invalid booking time.");

                    return;
                }


                // =================================================
                // CUSTOMER SESSION
                // =================================================

                Customer? customer =
                    await _sessionService.RestoreSessionAsync();

                if (customer == null ||
                    customer.CustomerID <= 0)
                {
                    await ShowErrorAsync(
                        "Login Required",
                        "Please log in before continuing.");

                    return;
                }


                // =================================================
                // LOAD ARENA
                // =================================================

                Arena? arena =
                    await _dbContext.Arenas
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x => x.ArenaID == _arenaID);

                if (arena == null)
                {
                    await ShowErrorAsync(
                        "Arena",
                        "The selected arena could not be found.");

                    return;
                }


                // =================================================
                // LOAD COURT
                // =================================================

                CourtOrMenuItem? court =
                    await _dbContext.CourtsAndMenuItems
                        .AsNoTracking()
                        .Include(x => x.Category)
                        .FirstOrDefaultAsync(
                            x =>
                                x.ItemID == _itemID &&
                                x.ArenaID == _arenaID);

                if (court == null)
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
                    court.CategoryID != _categoryID)
                {
                    await ShowErrorAsync(
                        "Sport",
                        "The selected court does not belong to the selected sport.");

                    return;
                }


                // =================================================
                // VERIFY COURT AVAILABILITY
                // =================================================

                if (!court.IsAvailable)
                {
                    await ShowErrorAsync(
                        "Court Unavailable",
                        "This court is currently unavailable.");

                    return;
                }


                // =================================================
                // CALCULATE HOURS
                // =================================================

                double hours =
                    (_endTime - _startTime).TotalHours;

                if (hours <= 0)
                {
                    await ShowErrorAsync(
                        "Booking Time",
                        "The selected booking duration is invalid.");

                    return;
                }


                // =================================================
                // CALCULATE PRICE
                // =================================================

                _courtPrice =
                    decimal.Round(
                        court.PricePerHour *
                        (decimal)hours,
                        2,
                        MidpointRounding.AwayFromZero);

                _serviceFee = 0m;

                _totalAmount =
                    _courtPrice +
                    _serviceFee;


                // =================================================
                // DISPLAY SUMMARY
                // =================================================

                SetBookingDetails(
                    arena,
                    court,
                    customer);


                // =================================================
                // DEFAULT PAYMENT
                // =================================================

                SetPayAtVenueSelected();
            }
            catch (OperationCanceledException)
            {
                // Safe cancellation.
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"LoadSummaryAsync failed: {ex}");

                await ShowErrorAsync(
                    "Booking Summary Error",
                    $"Unable to prepare the booking summary.\n\n{ex.Message}");
            }
            finally
            {
                SetLoadingState(false);

                _isLoading = false;
            }
        }


        // =========================================================
        // DISPLAY BOOKING DETAILS
        // =========================================================

        private void SetBookingDetails(
            Arena arena,
            CourtOrMenuItem court,
            Customer customer)
        {
            if (LblArenaName != null)
            {
                LblArenaName.Text =
                    string.IsNullOrWhiteSpace(arena.ArenaName)
                        ? "Arena"
                        : arena.ArenaName.Trim();
            }

            if (LblLocation != null)
            {
                LblLocation.Text =
                    string.IsNullOrWhiteSpace(arena.Location)
                        ? "Location unavailable"
                        : $"📍 {arena.Location.Trim()}";
            }

            if (LblSport != null)
            {
                LblSport.Text =
                    string.IsNullOrWhiteSpace(
                        court.Category?.CategoryName)
                        ? "Sport"
                        : court.Category.CategoryName.Trim();
            }

            if (LblCourt != null)
            {
                LblCourt.Text =
                    string.IsNullOrWhiteSpace(court.ItemName)
                        ? "Court"
                        : court.ItemName.Trim();
            }

            if (LblBookingDate != null)
            {
                LblBookingDate.Text =
                    _bookingDate.ToString(
                        "dddd, dd MMMM yyyy");
            }

            if (LblBookingTime != null)
            {
                LblBookingTime.Text =
                    $"{FormatTime(_startTime)} - " +
                    $"{FormatTime(_endTime)}";
            }

            if (LblCustomerName != null)
            {
                LblCustomerName.Text =
                    string.IsNullOrWhiteSpace(customer.FullName)
                        ? "Customer"
                        : customer.FullName.Trim();
            }

            if (LblCustomerPhone != null)
            {
                LblCustomerPhone.Text =
                    string.IsNullOrWhiteSpace(customer.PhoneNumber)
                        ? "Phone unavailable"
                        : customer.PhoneNumber.Trim();
            }

            if (LblCourtPrice != null)
            {
                LblCourtPrice.Text =
                    $"Rs. {_courtPrice:N0}";
            }

            if (LblServiceFee != null)
            {
                LblServiceFee.Text =
                    $"Rs. {_serviceFee:N0}";
            }

            if (LblTotalAmount != null)
            {
                LblTotalAmount.Text =
                    $"Rs. {_totalAmount:N0}";
            }
        }


        // =========================================================
        // PAYMENT RADIO BUTTON CHANGED
        // =========================================================

        private async void OnPaymentMethodChanged(
            object? sender,
            CheckedChangedEventArgs e)
        {
            try
            {
                if (!e.Value)
                    return;


                // =================================================
                // PAY AT VENUE
                // =================================================

                if (sender == RbPayAtVenue)
                {
                    SetPayAtVenueSelected();
                    return;
                }


                // =================================================
                // ONLINE PAYMENT
                // =================================================

                if (sender == RbOnlinePayment)
                {
                    SetOnlinePaymentSelected();

                    /*
                     * Online payment is visually selectable so
                     * the radio buttons work correctly.
                     *
                     * Actual online payment processing is not
                     * implemented yet.
                     */

                    await ShowErrorAsync(
                        "Online Payment",
                        "Online payment is not available yet. Please use Pay at Venue.");

                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Payment selection error: {ex}");
            }
        }


        // =========================================================
        // PAY AT VENUE TAPPED
        // =========================================================

        private void OnPayAtVenueTapped(
            object? sender,
            TappedEventArgs e)
        {
            try
            {
                SetPayAtVenueSelected();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Pay at venue tap error: {ex}");
            }
        }


        // =========================================================
        // ONLINE PAYMENT TAPPED
        // =========================================================

        private async void OnOnlinePaymentTapped(
            object? sender,
            TappedEventArgs e)
        {
            try
            {
                SetOnlinePaymentSelected();

                await ShowErrorAsync(
                    "Online Payment",
                    "Online payment is not available yet. Please use Pay at Venue.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Online payment tap error: {ex}");
            }
        }


        // =========================================================
        // SELECT PAY AT VENUE
        // =========================================================

        private void SetPayAtVenueSelected()
        {
            _paymentMethod = PayAtVenue;
            _paymentStatus = Pending;


            // =====================================================
            // RADIO BUTTONS
            // =====================================================

            if (RbPayAtVenue != null &&
                !RbPayAtVenue.IsChecked)
            {
                RbPayAtVenue.IsChecked = true;
            }

            if (RbOnlinePayment != null &&
                RbOnlinePayment.IsChecked)
            {
                RbOnlinePayment.IsChecked = false;
            }


            // =====================================================
            // PAY AT VENUE CARD
            // =====================================================

            if (PayAtVenueBorder != null)
            {
                PayAtVenueBorder.BackgroundColor =
                    Color.FromArgb("#12302D");

                PayAtVenueBorder.Stroke =
                    Color.FromArgb("#03DAC6");

                PayAtVenueBorder.StrokeThickness = 2;
            }


            // =====================================================
            // ONLINE PAYMENT CARD
            // =====================================================

            if (OnlinePaymentBorder != null)
            {
                OnlinePaymentBorder.BackgroundColor =
                    Color.FromArgb("#191B20");

                OnlinePaymentBorder.Stroke =
                    Color.FromArgb("#292D35");

                OnlinePaymentBorder.StrokeThickness = 1;
            }


            // =====================================================
            // NOTE
            // =====================================================

            if (LblPaymentNote != null)
            {
                LblPaymentNote.Text =
                    "You will pay at the arena when you arrive.";

                LblPaymentNote.TextColor =
                    Color.FromArgb("#03DAC6");
            }
        }


        // =========================================================
        // SELECT ONLINE PAYMENT
        // =========================================================

        private void SetOnlinePaymentSelected()
        {
            _paymentMethod = OnlinePayment;
            _paymentStatus = Pending;


            // =====================================================
            // RADIO BUTTONS
            // =====================================================

            if (RbOnlinePayment != null &&
                !RbOnlinePayment.IsChecked)
            {
                RbOnlinePayment.IsChecked = true;
            }

            if (RbPayAtVenue != null &&
                RbPayAtVenue.IsChecked)
            {
                RbPayAtVenue.IsChecked = false;
            }


            // =====================================================
            // ONLINE PAYMENT CARD
            // =====================================================

            if (OnlinePaymentBorder != null)
            {
                OnlinePaymentBorder.BackgroundColor =
                    Color.FromArgb("#12302D");

                OnlinePaymentBorder.Stroke =
                    Color.FromArgb("#03DAC6");

                OnlinePaymentBorder.StrokeThickness = 2;
            }


            // =====================================================
            // PAY AT VENUE CARD
            // =====================================================

            if (PayAtVenueBorder != null)
            {
                PayAtVenueBorder.BackgroundColor =
                    Color.FromArgb("#191B20");

                PayAtVenueBorder.Stroke =
                    Color.FromArgb("#292D35");

                PayAtVenueBorder.StrokeThickness = 1;
            }


            // =====================================================
            // NOTE
            // =====================================================

            if (LblPaymentNote != null)
            {
                LblPaymentNote.Text =
                    "Online payment is currently unavailable. Please select Pay at Venue to complete your booking.";

                LblPaymentNote.TextColor =
                    Color.FromArgb("#FFB74D");
            }
        }


        // =========================================================
        // CONFIRM BOOKING
        // =========================================================

        private async void OnConfirmBookingClicked(
            object? sender,
            EventArgs e)
        {
            if (_isConfirming)
                return;

            _isConfirming = true;

            if (BtnConfirmBooking != null)
            {
                BtnConfirmBooking.IsEnabled = false;
                BtnConfirmBooking.Text = "Confirming...";
            }

            try
            {
                // =================================================
                // VALIDATE QUERY
                // =================================================

                if (!_queryReceived)
                {
                    await ShowErrorAsync(
                        "Booking",
                        "Booking information has not loaded yet.");

                    return;
                }


                // =================================================
                // VALIDATE IDS
                // =================================================

                if (_arenaID <= 0 ||
                    _itemID <= 0)
                {
                    await ShowErrorAsync(
                        "Booking",
                        "The selected arena or court is invalid.");

                    return;
                }


                // =================================================
                // VALIDATE DATE
                // =================================================

                if (_bookingDate == default)
                {
                    await ShowErrorAsync(
                        "Booking",
                        "The booking date is invalid.");

                    return;
                }


                // =================================================
                // VALIDATE TIME
                // =================================================

                if (_endTime <= _startTime)
                {
                    await ShowErrorAsync(
                        "Booking",
                        "The booking time is invalid.");

                    return;
                }


                // =================================================
                // PAYMENT METHOD
                // =================================================

                if (!string.Equals(
                        _paymentMethod,
                        PayAtVenue,
                        StringComparison.Ordinal))
                {
                    await ShowErrorAsync(
                        "Payment Method",
                        "Online payment is not available yet. Please select Pay at Venue.");

                    SetPayAtVenueSelected();

                    return;
                }


                // =================================================
                // RESTORE CUSTOMER
                // =================================================

                Customer? customer =
                    await _sessionService.RestoreSessionAsync();

                if (customer == null ||
                    customer.CustomerID <= 0)
                {
                    await ShowErrorAsync(
                        "Login Required",
                        "Please log in before confirming the booking.");

                    return;
                }


                // =================================================
                // LOAD ARENA
                // =================================================

                Arena? arena =
                    await _dbContext.Arenas
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x => x.ArenaID == _arenaID);

                if (arena == null)
                {
                    await ShowErrorAsync(
                        "Arena",
                        "The selected arena could not be found.");

                    return;
                }


                // =================================================
                // LOAD COURT
                // =================================================

                CourtOrMenuItem? court =
                    await _dbContext.CourtsAndMenuItems
                        .AsNoTracking()
                        .Include(x => x.Category)
                        .FirstOrDefaultAsync(
                            x =>
                                x.ItemID == _itemID &&
                                x.ArenaID == _arenaID);

                if (court == null)
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
                    court.CategoryID != _categoryID)
                {
                    await ShowErrorAsync(
                        "Sport",
                        "The selected court does not belong to the selected sport.");

                    return;
                }


                // =================================================
                // VERIFY COURT AVAILABILITY
                // =================================================

                if (!court.IsAvailable)
                {
                    await ShowErrorAsync(
                        "Court Unavailable",
                        "This court is currently unavailable.");

                    return;
                }


                // =================================================
                // FINAL DURATION
                // =================================================

                double hours =
                    (_endTime - _startTime).TotalHours;

                if (hours <= 0)
                {
                    await ShowErrorAsync(
                        "Booking",
                        "Invalid booking duration.");

                    return;
                }


                // =================================================
                // FINAL PRICE
                // =================================================

                decimal finalPrice =
                    decimal.Round(
                        court.PricePerHour *
                        (decimal)hours,
                        2,
                        MidpointRounding.AwayFromZero);

                decimal serviceFee = 0m;

                decimal finalTotal =
                    finalPrice +
                    serviceFee;


                // =================================================
                // FINAL AVAILABILITY CHECK
                //
                // IMPORTANT:
                // Do NOT use:
                //
                // string.Equals(...,
                //     StringComparison.OrdinalIgnoreCase)
                //
                // inside EF Core.
                //
                // It cannot be translated to SQL.
                // =================================================

                bool alreadyBooked =
                    await _dbContext.Bookings
                        .AsNoTracking()
                        .AnyAsync(
                            booking =>
                                booking.ArenaID == _arenaID &&
                                booking.ItemID == _itemID &&
                                booking.BookingDate == _bookingDate &&

                                // SQL-translatable comparison
                                booking.BookingStatus != "Cancelled" &&

                                booking.StartTime < _endTime &&
                                booking.EndTime > _startTime);


                // =================================================
                // SLOT ALREADY BOOKED
                // =================================================

                if (alreadyBooked)
                {
                    await ShowErrorAsync(
                        "Time Unavailable",
                        "This time slot has just been booked by another customer. Please select another time.");

                    return;
                }


                // =================================================
                // CREATE BOOKING
                // =================================================

                Booking booking =
                    new Booking
                    {
                        ArenaID = _arenaID,

                        ItemID = _itemID,

                        CustomerName =
                            customer.FullName?.Trim()
                            ?? string.Empty,

                        CustomerPhone =
                            customer.PhoneNumber?.Trim()
                            ?? string.Empty,

                        BookingDate = _bookingDate,

                        StartTime = _startTime,

                        EndTime = _endTime,

                        TotalAmount = finalTotal,

                        BookingStatus = "Confirmed",

                        PaymentStatus = Pending,

                        PaymentMethod = PayAtVenue,

                        RefundStatus = "Not Required"
                    };


                // =================================================
                // SAVE BOOKING
                // =================================================

                await _dbContext.Bookings.AddAsync(booking);

                await _dbContext.SaveChangesAsync();


                // =================================================
                // VERIFY DATABASE GENERATED ID
                // =================================================

                if (booking.BookingID <= 0)
                {
                    await ShowErrorAsync(
                        "Booking Error",
                        "The booking was saved, but a valid booking ID was not generated.");

                    return;
                }


                // =================================================
                // CREATE NOTIFICATION
                // =================================================

                try
                {
                    await _notificationService
                        .CreateBookingConfirmationNotificationAsync(
                            customer,
                            booking,
                            arena,
                            court,
                            court.Category);
                }
                catch (Exception notificationException)
                {
                    /*
                     * Booking has already been successfully saved.
                     *
                     * A notification failure must NOT delete or
                     * invalidate the booking.
                     */

                    System.Diagnostics.Debug.WriteLine(
                        $"Booking notification failed: {notificationException}");
                }


                // =================================================
                // NAVIGATION
                // =================================================

                if (Shell.Current == null)
                {
                    await ShowErrorAsync(
                        "Navigation",
                        "The application navigation system is unavailable.");

                    return;
                }


                await Shell.Current.GoToAsync(
                    "CustomerBookingConfirmationPage",
                    new Dictionary<string, object>
                    {
                        ["BookingID"] = booking.BookingID,

                        ["ArenaID"] = _arenaID,

                        ["CategoryID"] = _categoryID,

                        ["ItemID"] = _itemID,

                        ["BookingDate"] = _bookingDate,

                        ["StartTime"] = _startTime,

                        ["EndTime"] = _endTime,

                        ["TotalAmount"] = finalTotal,

                        ["PaymentMethod"] = PayAtVenue,

                        ["PaymentStatus"] = Pending
                    });
            }
            catch (DbUpdateException dbException)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Booking database error: {dbException}");

                string databaseMessage =
                    dbException.InnerException?.Message
                    ?? dbException.Message;

                await ShowErrorAsync(
                    "Booking Error",
                    "The booking could not be saved to the database.\n\n" +
                    databaseMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Confirm booking failed: {ex}");

                await ShowErrorAsync(
                    "Booking Error",
                    $"Unable to confirm the booking.\n\n{ex.Message}");
            }
            finally
            {
                _isConfirming = false;

                if (BtnConfirmBooking != null)
                {
                    BtnConfirmBooking.IsEnabled = true;
                    BtnConfirmBooking.Text = "Confirm Booking";
                }
            }
        }


        // =========================================================
        // RESET DISPLAY
        // =========================================================

        private void ResetDisplay()
        {
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

            if (LblCourtPrice != null)
                LblCourtPrice.Text = "Rs. 0";

            if (LblServiceFee != null)
                LblServiceFee.Text = "Rs. 0";

            if (LblTotalAmount != null)
                LblTotalAmount.Text = "Rs. 0";


            if (BtnConfirmBooking != null)
            {
                BtnConfirmBooking.IsEnabled = false;
                BtnConfirmBooking.Text = "Confirm Booking";
            }


            // Default payment selection
            SetPayAtVenueSelected();
        }


        // =========================================================
        // LOADING STATE
        // =========================================================

        private void SetLoadingState(bool isLoading)
        {
            if (LoadingIndicator != null)
            {
                LoadingIndicator.IsVisible = isLoading;
                LoadingIndicator.IsRunning = isLoading;
            }


            if (isLoading)
            {
                if (BtnConfirmBooking != null)
                    BtnConfirmBooking.IsEnabled = false;
            }
            else
            {
                if (BtnConfirmBooking != null &&
                    _queryReceived &&
                    !_isConfirming)
                {
                    BtnConfirmBooking.IsEnabled = true;
                }
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
        // DATE ONLY QUERY VALUE
        // =========================================================

        private static DateOnly GetDateOnlyValue(
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
                return default;
            }

            if (value is DateOnly dateOnly)
                return dateOnly;

            if (value is DateTime dateTime)
                return DateOnly.FromDateTime(dateTime);

            if (value is DateTimeOffset dateTimeOffset)
                return DateOnly.FromDateTime(
                    dateTimeOffset.DateTime);

            return DateOnly.TryParse(
                value.ToString(),
                out DateOnly parsedDate)
                    ? parsedDate
                    : default;
        }


        // =========================================================
        // TIME SPAN QUERY VALUE
        // =========================================================

        private static TimeSpan GetTimeSpanValue(
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
                return TimeSpan.Zero;
            }

            if (value is TimeSpan timeSpan)
                return timeSpan;

            if (value is DateTime dateTime)
                return dateTime.TimeOfDay;

            if (value is DateTimeOffset dateTimeOffset)
                return dateTimeOffset.TimeOfDay;

            return TimeSpan.TryParse(
                value.ToString(),
                out TimeSpan parsedTime)
                    ? parsedTime
                    : TimeSpan.Zero;
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
        // BACK
        // =========================================================

        private async void OnBackTapped(
            object? sender,
            TappedEventArgs e)
        {
            try
            {
                if (_isConfirming)
                    return;

                if (Shell.Current == null)
                {
                    await ShowErrorAsync(
                        "Navigation Error",
                        "Navigation is currently unavailable.");

                    return;
                }

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Back navigation failed: {ex}");

                await ShowErrorAsync(
                    "Navigation Error",
                    "Unable to go back.");
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
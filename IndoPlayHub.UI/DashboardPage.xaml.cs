
using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using Microsoft.EntityFrameworkCore;

namespace IndoPlayHub.UI
{
    public partial class DashboardPage : ContentPage
    {
        private readonly AppDbContext _dbContext;
        private readonly int _currentArenaId;

        private bool _isLoading;

        public DashboardPage(
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

            await LoadDashboardAsync();
        }

        // =========================================================
        // MAIN DASHBOARD LOAD
        // =========================================================

        private async Task LoadDashboardAsync()
        {
            if (_isLoading)
                return;

            _isLoading = true;

            try
            {
                // =================================================
                // VALIDATE ARENA
                // =================================================

                if (_currentArenaId <= 0)
                {
                    await DisplayAlertAsync(
                        "Dashboard Error",
                        "Invalid arena ID.",
                        "OK");

                    return;
                }

                // =================================================
                // LOAD ARENA
                // =================================================

                var arena = await _dbContext.Arenas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a =>
                        a.ArenaID == _currentArenaId);

                if (arena == null)
                {
                    await DisplayAlertAsync(
                        "Dashboard Error",
                        "The selected arena could not be found.",
                        "OK");

                    return;
                }

                // =================================================
                // ARENA INFORMATION
                // =================================================

                LblWelcome.Text =
                    $"Welcome back, {arena.OwnerName}!";

                LblArenaSubtitle.Text =
                    "Monitor your arena's daily performance and bookings.";

                LblArenaName.Text =
                    string.IsNullOrWhiteSpace(arena.ArenaName)
                        ? "My Arena"
                        : arena.ArenaName;

                LblArenaLocation.Text =
                    $"📍 {(string.IsNullOrWhiteSpace(arena.Location)
                        ? "Location not set"
                        : arena.Location)}";

                LblBusinessHours.Text =
                    $"{arena.OpeningTime:hh\\:mm} - {arena.ClosingTime:hh\\:mm}";

                LblSetupStatus.Text =
                    arena.IsSetupCompleted
                        ? "● Active"
                        : "● Setup Incomplete";

                LblSetupStatus.TextColor =
                    arena.IsSetupCompleted
                        ? Color.FromArgb("#03DAC6")
                        : Color.FromArgb("#FFB74D");

                // =================================================
                // ARENA HERO IMAGE
                // =================================================

                ImgArenaCover.Source = "arena_bg.jpg";

                // =================================================
                // LOAD COURTS / MENU ITEMS
                // =================================================

                var courts = await _dbContext.CourtsAndMenuItems
                    .AsNoTracking()
                    .Include(x => x.Category)
                    .Where(x =>
                        x.ArenaID == _currentArenaId)
                    .OrderBy(x =>
                        x.Category != null
                            ? x.Category.CategoryName
                            : "")
                    .ThenBy(x => x.ItemName)
                    .ToListAsync();

                // =================================================
                // COURT METRICS
                // =================================================

                int totalCourts =
                    courts.Count;

                int availableCourts =
                    courts.Count(c => c.IsAvailable);

                LblTotalCourts.Text =
                    totalCourts.ToString();

                LblAvailableCourts.Text =
                    availableCourts.ToString();

                // =================================================
                // TODAY
                // =================================================

                DateOnly today =
                    DateOnly.FromDateTime(DateTime.Today);

                DateOnly tomorrow =
                    today.AddDays(1);

                // =================================================
                // TODAY'S BOOKINGS
                // =================================================

                var todayBookings =
                    await _dbContext.Bookings
                        .AsNoTracking()
                        .Include(b => b.CourtOrMenuItem)
                        .Where(b =>
                            b.ArenaID == _currentArenaId &&
                            b.BookingDate >= today &&
                            b.BookingDate < tomorrow)
                        .OrderBy(b => b.StartTime)
                        .ToListAsync();

                // =================================================
                // TODAY'S BOOKING COUNT
                // =================================================

                LblTodayBookings.Text =
                    todayBookings.Count.ToString();

                // =================================================
                // TODAY'S REVENUE
                // =================================================

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

                // =================================================
                // PENDING PAYMENTS
                // =================================================

                int pendingPayments =
                    todayBookings.Count(b =>
                        string.Equals(
                            b.PaymentStatus,
                            "Pending",
                            StringComparison.OrdinalIgnoreCase));

                LblPendingPayments.Text =
                    pendingPayments.ToString();

                // =================================================
                // UNIQUE CUSTOMERS
                // =================================================

                int uniqueCustomers =
                    todayBookings
                        .Select(b => b.CustomerPhone)
                        .Where(phone =>
                            !string.IsNullOrWhiteSpace(phone))
                        .Distinct()
                        .Count();

                LblTodayCustomers.Text =
                    uniqueCustomers.ToString();

                // =================================================
                // BUILD SPORT CARDS
                // =================================================

                var sportDisplayItems =
                    BuildSportDashboardItems(
                        courts,
                        todayBookings,
                        arena);

                CvSports.ItemsSource =
                    sportDisplayItems;

                // =================================================
                // TODAY'S BOOKINGS DISPLAY
                // =================================================

                var bookingDisplayItems =
                    todayBookings
                        .Select(b =>
                            new BookingDashboardItem
                            {
                                BookingID =
                                    b.BookingID,

                                CustomerName =
                                    string.IsNullOrWhiteSpace(
                                        b.CustomerName)
                                        ? "Unknown Customer"
                                        : b.CustomerName,

                                CustomerPhone =
                                    b.CustomerPhone ?? "",

                                CourtName =
                                    b.CourtOrMenuItem?.ItemName
                                    ?? "Unknown Court",

                                TimeRange =
                                    $"{b.StartTime:hh\\:mm} - {b.EndTime:hh\\:mm}",

                                TotalAmount =
                                    b.TotalAmount,

                                BookingStatus =
                                    b.BookingStatus
                                    ?? "Unknown",

                                PaymentStatus =
                                    b.PaymentStatus
                                    ?? "Unknown",

                                PaymentColor =
                                    GetPaymentColor(
                                        b.PaymentStatus)
                            })
                        .ToList();

                CvTodayBookings.ItemsSource =
                    bookingDisplayItems;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Dashboard Error",
                    $"Failed to load dashboard data:\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                _isLoading = false;
            }
        }

        // =========================================================
        // BUILD SPORT DASHBOARD CARDS
        // =========================================================

        private List<SportDashboardItem> BuildSportDashboardItems(
            List<CourtOrMenuItem> courts,
            List<Booking> todayBookings,
            Arena arena)
        {
            var result =
                new List<SportDashboardItem>();

            // =====================================================
            // GROUP COURTS BY SPORT / CATEGORY
            // =====================================================

            var groupedSports =
                courts
                    .Where(c =>
                        c.Category != null &&
                        !string.IsNullOrWhiteSpace(
                            c.Category.CategoryName))
                    .GroupBy(c =>
                        c.Category!.CategoryName.Trim())
                    .OrderBy(g => g.Key);

            foreach (var group in groupedSports)
            {
                string sportName =
                    group.Key;

                var sportCourts =
                    group.ToList();

                // =================================================
                // COURT COUNTS
                // =================================================

                int totalCourts =
                    sportCourts.Count;

                int availableCourts =
                    sportCourts.Count(c =>
                        c.IsAvailable);

                // =================================================
                // BOOKINGS FOR THIS SPORT
                // =================================================

                var sportCourtIds =
                    sportCourts
                        .Select(c => c.ItemID)
                        .ToHashSet();

                var sportBookings =
                    todayBookings
                        .Where(b =>
                            b.CourtOrMenuItem != null &&
                            sportCourtIds.Contains(
                                b.CourtOrMenuItem.ItemID))
                        .OrderBy(b => b.StartTime)
                        .ToList();

                // =================================================
                // BOOKED COURTS
                // =================================================

                int bookedCourts =
                    sportBookings
                        .Select(b =>
                            b.CourtOrMenuItem?.ItemID)
                        .Where(id =>
                            id.HasValue)
                        .Distinct()
                        .Count();

                // =================================================
                // AVERAGE COURT RATE
                // =================================================

                decimal averageRate =
                    sportCourts.Count > 0
                        ? sportCourts.Average(c =>
                            c.PricePerHour)
                        : 0;

                // =================================================
                // TODAY'S SCHEDULE
                // =================================================

                string todayScheduleText;

                if (sportBookings.Count == 0)
                {
                    todayScheduleText =
                        "No bookings scheduled today.";
                }
                else
                {
                    todayScheduleText =
                        string.Join(
                            "  •  ",
                            sportBookings
                                .Take(4)
                                .Select(b =>
                                    $"{b.StartTime:hh\\:mm}-{b.EndTime:hh\\:mm} " +
                                    $"{b.CourtOrMenuItem?.ItemName ?? "Court"}"));
                }

                // =================================================
                // UPCOMING BOOKINGS
                // =================================================

                var upcomingBookings =
                    sportBookings
                        .Where(b =>
                            b.StartTime >=
                            DateTime.Now.TimeOfDay)
                        .Take(3)
                        .ToList();

                string upcomingBookingsText;

                if (upcomingBookings.Count == 0)
                {
                    upcomingBookingsText =
                        "No upcoming bookings.";
                }
                else
                {
                    upcomingBookingsText =
                        string.Join(
                            "  •  ",
                            upcomingBookings.Select(b =>
                                $"{b.StartTime:hh\\:mm} " +
                                $"{b.CustomerName ?? "Customer"}"));
                }

                // =================================================
                // GET THE CORRECT SPORT IMAGE
                // =================================================

                string imageSource =
                    GetSportImage(sportName);

                // =================================================
                // CREATE DASHBOARD CARD
                // =================================================

                result.Add(
                    new SportDashboardItem
                    {
                        CategoryID =
                            group.First().CategoryID,

                        SportName =
                            sportName,

                        ArenaName =
                            string.IsNullOrWhiteSpace(
                                arena.ArenaName)
                                ? "My Arena"
                                : arena.ArenaName,

                        LocationText =
                            string.IsNullOrWhiteSpace(
                                arena.Location)
                                ? "Location not set"
                                : arena.Location,

                        ImageSource =
                            imageSource,

                        TotalCourts =
                            totalCourts,

                        AvailableCourts =
                            availableCourts,

                        BookedCourts =
                            bookedCourts,

                        BusinessHours =
                            $"{arena.OpeningTime:hh\\:mm} - " +
                            $"{arena.ClosingTime:hh\\:mm}",

                        CourtRateText =
                            $"Rs. {averageRate:N0} / hour",

                        TodayScheduleCountText =
                            sportBookings.Count == 1
                                ? "1 booking"
                                : $"{sportBookings.Count} bookings",

                        TodayScheduleText =
                            todayScheduleText,

                        UpcomingBookingsText =
                            upcomingBookingsText
                    });
            }

            return result;
        }

        // =========================================================
        // SPORT IMAGE MAPPING
        // =========================================================

        private string GetSportImage(string? sportName)
        {
            if (string.IsNullOrWhiteSpace(sportName))
                return "arena_bg.jpg";

            string sport =
                sportName.Trim().ToLowerInvariant();

            // Indoor Futsal
            if (sport == "indoor futsal" ||
                sport.Contains("futsal"))
                return "futsal.jpg";

            // Indoor Football / Football
            if (sport == "indoor football" ||
                sport == "football" ||
                sport.Contains("football"))
                return "football.jpg";

            // Soccer / Indoor Soccer
            if (sport == "indoor soccer" ||
                sport == "soccer" ||
                sport.Contains("soccer"))
                return "indoorsoccer.jpg";

            // Indoor Cricket / Cricket
            if (sport == "indoor cricket" ||
                sport == "cricket" ||
                sport.Contains("cricket"))
                return "cricket.jpg";

            // Badminton
            if (sport == "badminton" ||
                sport.Contains("badminton"))
                return "badminton.jpg";

            // Table Tennis
            if (sport == "table tennis" ||
                sport.Contains("table tennis") ||
                sport.Contains("tabletennis") ||
                sport.Contains("ping pong"))
                return "tabletennis.jpg";

            // Basketball
            if (sport == "basketball" ||
                sport.Contains("basketball"))
                return "basketball.jpg";

            // Squash
            if (sport == "squash" ||
                sport.Contains("squash"))
                return "squash.jpg";

            // Indoor Volleyball
            if (sport == "indoor volleyball" ||
                sport.Contains("volleyball"))
                return "volleyball.jpg";

            // Billiards & Pool
            if (sport == "billiards & pool" ||
                sport.Contains("billiards") ||
                sport.Contains("pool"))
                return "billiardsandpool.jpg";

            // Indoor Boxing
            if (sport == "indoor boxing" ||
                sport.Contains("boxing"))
                return "boxing.jpg";

            // Fitness & Gym
            if (sport == "fitness & gym" ||
                sport.Contains("fitness") ||
                sport.Contains("gym"))
                return "fitnessandgym.jpg";

            // Snooker Room
            if (sport == "snooker room" ||
                sport.Contains("snooker"))
                return "snooker.jpg";

            // Yoga & Pilates
            if (sport == "yoga & pilates" ||
                sport.Contains("yoga") ||
                sport.Contains("pilates"))
                return "yogaandpilates.jpg";

            // Indoor Board Games
            if (sport == "indoor board games" ||
                sport.Contains("board game") ||
                sport.Contains("boardgame"))
                return "boardgames.jpg";

            // Indoor Climbing
            if (sport == "indoor climbing" ||
                sport.Contains("climbing"))
                return "climbing.jpg";

            // Martial Arts Dojo
            if (sport == "martial arts dojo" ||
                sport.Contains("martial") ||
                sport.Contains("karate") ||
                sport.Contains("taekwondo") ||
                sport.Contains("dojo"))
                return "martialartsdojo.jpg";

            // Unknown category
            return "arena_bg.jpg";
        }

        // =========================================================
        // PAYMENT STATUS COLOR
        // =========================================================

        private Color GetPaymentColor(
            string? paymentStatus)
        {
            if (string.Equals(
                paymentStatus,
                "Paid",
                StringComparison.OrdinalIgnoreCase))
            {
                return Color.FromArgb("#03DAC6");
            }

            if (string.Equals(
                paymentStatus,
                "Pending",
                StringComparison.OrdinalIgnoreCase))
            {
                return Color.FromArgb("#FFB74D");
            }

            if (string.Equals(
                paymentStatus,
                "Failed",
                StringComparison.OrdinalIgnoreCase))
            {
                return Color.FromArgb("#FF5252");
            }

            return Color.FromArgb("#9AA4B2");
        }

        // =========================================================
        // NOTIFICATIONS
        // =========================================================

        private async void OnNotificationsClicked(
            object? sender,
            TappedEventArgs e)
        {
            try
            {
                if (_currentArenaId <= 0)
                {
                    await DisplayAlertAsync(
                        "Notification Error",
                        "Invalid arena ID.",
                        "OK");

                    return;
                }

                int pendingPayments =
                    await _dbContext.Bookings
                        .AsNoTracking()
                        .CountAsync(b =>
                            b.ArenaID == _currentArenaId &&
                            b.PaymentStatus == "Pending");

                await DisplayAlertAsync(
                    "Notifications",
                    pendingPayments > 0
                        ? $"You have {pendingPayments} booking payment(s) awaiting payment."
                        : "You have no pending payment notifications.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Notification Error",
                    ex.Message,
                    "OK");
            }
        }

        // =========================================================
        // GO TO COURTS
        // =========================================================

        private async void OnGoToCourtsClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                if (_currentArenaId <= 0)
                {
                    await DisplayAlertAsync(
                        "Navigation Error",
                        "Invalid arena ID.",
                        "OK");

                    return;
                }

                await Navigation.PushAsync(
                    new CourtsPage(
                        _dbContext,
                        _currentArenaId));
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Navigation Error",
                    $"Could not open courts management:\n\n{ex.Message}",
                    "OK");
            }
        }

        // =========================================================
        // MANAGE SPORTS
        // =========================================================

        private async void OnManageSportsClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                if (_currentArenaId <= 0)
                {
                    await DisplayAlertAsync(
                        "Navigation Error",
                        "Invalid arena ID.",
                        "OK");

                    return;
                }

                await Navigation.PushAsync(
                    new ManageSportsPage(
                        _dbContext,
                        _currentArenaId));
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Navigation Error",
                    $"Could not open sport management:\n\n{ex.Message}",
                    "OK");
            }
        }

        // =========================================================
        // GO TO MENU
        // =========================================================

        private async void OnGoToMenuClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                if (_currentArenaId <= 0)
                {
                    await DisplayAlertAsync(
                        "Navigation Error",
                        "Invalid arena ID.",
                        "OK");

                    return;
                }

                await Navigation.PushAsync(
                    new MenuPage(
                        _dbContext,
                        _currentArenaId));
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Navigation Error",
                    $"Could not open menu management:\n\n{ex.Message}",
                    "OK");
            }
        }

        // =========================================================
        // GO TO BOOKINGS
        // =========================================================

        private async void OnGoToBookingsClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                if (_currentArenaId <= 0)
                {
                    await DisplayAlertAsync(
                        "Navigation Error",
                        "Invalid arena ID.",
                        "OK");

                    return;
                }

                await Navigation.PushAsync(
                    new BookingsPage(
                        _dbContext,
                        _currentArenaId));
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Navigation Error",
                    $"Could not open bookings:\n\n{ex.Message}",
                    "OK");
            }
        }

        // =========================================================
        // EDIT BUSINESS DETAILS
        // =========================================================

        private async void OnEditBusinessDetailsClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                if (_currentArenaId <= 0)
                {
                    await DisplayAlertAsync(
                        "Login Required",
                        "Please log in first.",
                        "OK");

                    return;
                }

                bool arenaExists =
                    await _dbContext.Arenas
                        .AsNoTracking()
                        .AnyAsync(a =>
                            a.ArenaID == _currentArenaId);

                if (!arenaExists)
                {
                    await DisplayAlertAsync(
                        "Account Error",
                        "Your arena account could not be found. Please log in again.",
                        "OK");

                    return;
                }

                await Navigation.PushAsync(
                    new EditBusinessDetailsPage(
                        _dbContext,
                        _currentArenaId));
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Navigation Error",
                    $"Could not open business details:\n\n{ex.Message}",
                    "OK");
            }
        }

        // =========================================================
        // EDIT INDIVIDUAL SPORT
        // =========================================================

        private async void OnEditSportClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                if (sender is not Button button)
                    return;

                if (button.BindingContext
                    is not SportDashboardItem sport)
                    return;

                if (sport.CategoryID <= 0)
                {
                    await DisplayAlertAsync(
                        "Sport Error",
                        "Could not identify this sport.",
                        "OK");

                    return;
                }

                if (_currentArenaId <= 0)
                {
                    await DisplayAlertAsync(
                        "Sport Error",
                        "Invalid arena ID.",
                        "OK");

                    return;
                }

                await Navigation.PushAsync(
                    new EditSportPage(
                        _dbContext,
                        _currentArenaId,
                        sport.CategoryID));
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Navigation Error",
                    $"Could not open sport management:\n\n{ex.Message}",
                    "OK");
            }
        }

        // =========================================================
        // SPORT DASHBOARD MODEL
        // =========================================================

        private class SportDashboardItem
        {
            public int CategoryID { get; set; }

            public string SportName { get; set; } =
                string.Empty;

            public string ArenaName { get; set; } =
                string.Empty;

            public string LocationText { get; set; } =
                string.Empty;

            public string ImageSource { get; set; } =
                "arena_bg.jpg";

            public int TotalCourts { get; set; }

            public int AvailableCourts { get; set; }

            public int BookedCourts { get; set; }

            public string BusinessHours { get; set; } =
                string.Empty;

            public string CourtRateText { get; set; } =
                string.Empty;

            public string TodayScheduleCountText { get; set; } =
                string.Empty;

            public string TodayScheduleText { get; set; } =
                string.Empty;

            public string UpcomingBookingsText { get; set; } =
                string.Empty;
        }

        // =========================================================
        // BOOKING DISPLAY MODEL
        // =========================================================

        private class BookingDashboardItem
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


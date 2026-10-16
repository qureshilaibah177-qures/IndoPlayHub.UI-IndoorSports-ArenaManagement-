using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using Microsoft.EntityFrameworkCore;

namespace IndoPlayHub.UI
{
    public partial class ManageSportsPage : ContentPage
    {
        private readonly AppDbContext _dbContext;
        private readonly int _currentArenaId;

        private bool _isLoading;

        public ManageSportsPage(
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

            await LoadSportsAsync();
        }

        // =========================================================
        // LOAD SPORTS
        // =========================================================

        private async Task LoadSportsAsync()
        {
            if (_isLoading)
                return;

            _isLoading = true;

            try
            {
                if (_currentArenaId <= 0)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "Invalid arena ID.",
                        "OK");

                    return;
                }

                // =================================================
                // LOAD ARENA
                // =================================================

                var arena =
                    await _dbContext.Arenas
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
                // LOAD COURTS
                // =================================================

                var courts =
                    await _dbContext.CourtsAndMenuItems
                        .AsNoTracking()
                        .Include(c => c.Category)
                        .Where(c =>
                            c.ArenaID == _currentArenaId &&
                            c.Category != null)
                        .OrderBy(c =>
                            c.Category!.CategoryName)
                        .ThenBy(c =>
                            c.ItemName)
                        .ToListAsync();

                // =================================================
                // GROUP BY SPORT
                // =================================================

                var sports =
                    courts
                        .Where(c =>
                            c.Category != null &&
                            !string.IsNullOrWhiteSpace(
                                c.Category.CategoryName))
                        .GroupBy(c =>
                            c.Category!.CategoryID)
                        .Select(group =>
                        {
                            var firstCourt =
                                group.First();

                            string sportName =
                                firstCourt.Category!.CategoryName.Trim();

                            int totalUnits =
                                group.Count();

                            int availableUnits =
                                group.Count(c =>
                                    c.IsAvailable);

                            decimal averageRate =
                                totalUnits > 0
                                    ? group.Average(c =>
                                        c.PricePerHour)
                                    : 0;

                            return new SportManagementItem
                            {
                                CategoryID =
                                    firstCourt.CategoryID,

                                SportName =
                                    sportName,

                                SportIcon =
                                    GetSportIcon(
                                        sportName),

                                TotalUnits =
                                    totalUnits,

                                AvailableUnits =
                                    availableUnits,

                                UnitText =
                                    totalUnits == 1
                                        ? "1 playing unit"
                                        : $"{totalUnits} playing units",

                                AvailableUnitsText =
                                    $"{availableUnits} of {totalUnits} available",

                                RateText =
                                    $"Rs. {averageRate:N0} / hour",

                                BusinessHours =
                                    $"{arena.OpeningTime:hh\\:mm} - " +
                                    $"{arena.ClosingTime:hh\\:mm}"
                            };
                        })
                        .OrderBy(s =>
                            s.SportName)
                        .ToList();

                // =================================================
                // SUMMARY
                // =================================================

                LblTotalSports.Text =
                    sports.Count.ToString();

                LblTotalUnits.Text =
                    courts.Count.ToString();

                LblArenaHours.Text =
                    $"{arena.OpeningTime:hh\\:mm} - " +
                    $"{arena.ClosingTime:hh\\:mm}";

                // =================================================
                // DISPLAY
                // =================================================

                CvSports.ItemsSource =
                    sports;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Sports Management Error",
                    $"Failed to load sports:\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                _isLoading = false;
            }
        }

        // =========================================================
        // ADD SPORT
        // =========================================================

        private async void OnAddSportClicked(
    object? sender,
    EventArgs e)
        {
            try
            {
                if (_currentArenaId <= 0)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "Invalid arena ID.",
                        "OK");

                    return;
                }

                await Navigation.PushAsync(
                    new AddSportPage(
                        _dbContext,
                        _currentArenaId));
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Navigation Error",
                    $"Could not open Add Sport:\n\n{ex.Message}",
                    "OK");
            }
        }

        // =========================================================
        // EDIT SPORT
        // =========================================================

        private async void OnEditSportClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                if (sender is not Button button)
                    return;

                if (button.CommandParameter
                    is not SportManagementItem sport)
                    return;

                // We will connect this to EditSportPage
                // in the next step.

                await DisplayAlertAsync(
                    "Sport Selected",
                    $"You selected {sport.SportName}.\n\n" +
                    $"Units: {sport.TotalUnits}\n" +
                    $"Category ID: {sport.CategoryID}",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    ex.Message,
                    "OK");
            }
        }

        // =========================================================
        // SPORT ICON
        // =========================================================

        private string GetSportIcon(
            string? sportName)
        {
            if (string.IsNullOrWhiteSpace(sportName))
                return "🏟️";

            string sport =
                sportName.Trim().ToLowerInvariant();

            if (sport.Contains("cricket"))
                return "🏏";

            if (sport.Contains("basketball"))
                return "🏀";

            if (sport.Contains("badminton"))
                return "🏸";

            if (sport.Contains("football") ||
                sport.Contains("futsal"))
                return "⚽";

            if (sport.Contains("volleyball"))
                return "🏐";

            if (sport.Contains("table tennis") ||
                sport.Contains("tabletennis") ||
                sport.Contains("ping pong"))
                return "🏓";

            if (sport.Contains("tennis"))
                return "🎾";

            if (sport.Contains("boxing"))
                return "🥊";

            if (sport.Contains("gym") ||
                sport.Contains("fitness"))
                return "🏋️";

            if (sport.Contains("snooker"))
                return "🎱";

            if (sport.Contains("billiard") ||
                sport.Contains("pool"))
                return "🎱";

            if (sport.Contains("yoga"))
                return "🧘";

            if (sport.Contains("climbing"))
                return "🧗";

            if (sport.Contains("martial") ||
                sport.Contains("karate") ||
                sport.Contains("taekwondo"))
                return "🥋";

            return "🏟️";
        }

        // =========================================================
        // DISPLAY MODEL
        // =========================================================

        private class SportManagementItem
        {
            public int CategoryID { get; set; }

            public string SportName { get; set; } =
                string.Empty;

            public string SportIcon { get; set; } =
                "🏟️";

            public int TotalUnits { get; set; }

            public int AvailableUnits { get; set; }

            public string UnitText { get; set; } =
                string.Empty;

            public string AvailableUnitsText { get; set; } =
                string.Empty;

            public string RateText { get; set; } =
                string.Empty;

            public string BusinessHours { get; set; } =
                string.Empty;
        }
    }
}
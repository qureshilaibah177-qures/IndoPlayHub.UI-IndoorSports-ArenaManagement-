using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

using ArenaItemModel =
    IndoPlayHub.UI.Models.CustomerArenaItem;

namespace IndoPlayHub.UI
{
    public partial class CustomerSportArenasPage :
        ContentPage,
        IQueryAttributable
    {
        // =========================================================
        // DATABASE
        // =========================================================

        private readonly AppDbContext _dbContext;


        // =========================================================
        // SELECTED SPORT
        // =========================================================

        private int _categoryID;

        private string _categoryName = "Sport";


        // =========================================================
        // ARENA COLLECTIONS
        // =========================================================

        private readonly ObservableCollection<ArenaItemModel> _arenas =
            new();

        private List<ArenaItemModel> _filteredArenas =
            new();


        // =========================================================
        // LOADING STATE
        // =========================================================

        private bool _isLoading;


        // =========================================================
        // FILTER / SORT STATE
        // =========================================================

        private bool _availableOnly;

        private bool _openNowOnly;

        private decimal? _maximumPrice;

        private string _sortOption = "Recommended";


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public CustomerSportArenasPage(
            AppDbContext dbContext)
        {
            InitializeComponent();

            _dbContext = dbContext;

            // -----------------------------------------------------
            // Connect collection to CollectionView
            // -----------------------------------------------------

            CvArenas.ItemsSource = _arenas;

            // -----------------------------------------------------
            // Default sorting
            // -----------------------------------------------------

            SortPicker.SelectedIndex = 0;
        }


        // =========================================================
        // RECEIVE SHELL QUERY PARAMETERS
        // =========================================================

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            try
            {
                // =================================================
                // CATEGORY ID
                // =================================================

                if (query.TryGetValue(
                    "CategoryID",
                    out object? categoryID))
                {
                    if (categoryID is int id)
                    {
                        _categoryID = id;
                    }
                    else if (
                        int.TryParse(
                            categoryID?.ToString(),
                            out int parsedID))
                    {
                        _categoryID = parsedID;
                    }
                }


                // =================================================
                // CATEGORY NAME
                // =================================================

                if (query.TryGetValue(
                    "CategoryName",
                    out object? categoryName))
                {
                    string? name =
                        categoryName?
                            .ToString()?
                            .Trim();

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        _categoryName = name;
                    }
                }


                // =================================================
                // UPDATE HEADER
                // =================================================

                LblSportTitle.Text =
                    $"{_categoryName} Arenas";

                LblSubtitle.Text =
                    $"Discover arenas offering {_categoryName}.";


                // =================================================
                // LOAD ARENAS
                // =================================================

                _ = LoadArenasAsync();
            }
            catch (Exception ex)
            {
                _ = DisplayAlertAsync(
                    "Loading Error",
                    $"Unable to process sport selection.\n\n{ex.Message}",
                    "OK");
            }
        }


        // =========================================================
        // LOAD ARENAS
        // =========================================================

        private async Task LoadArenasAsync()
        {
            // -----------------------------------------------------
            // Prevent duplicate loading
            // -----------------------------------------------------

            if (_isLoading)
                return;

            _isLoading = true;

            try
            {
                // =================================================
                // VALIDATE SPORT
                // =================================================

                if (_categoryID <= 0)
                {
                    ShowEmptyState(
                        "Invalid sport selection.");

                    return;
                }


                // =================================================
                // LOAD ALL UNITS FOR THIS SPORT
                // =================================================
                //
                // IMPORTANT:
                // Do NOT put x.IsAvailable here.
                //
                // We need ALL units so that we can display:
                //
                // Sport Climbing
                // 3 units • 2 available
                //
                // instead of hiding the unavailable units.
                //
                // =================================================

                var items =
                    await _dbContext.CourtsAndMenuItems
                        .AsNoTracking()
                        .Include(x => x.Arena)
                        .Where(x =>
                            x.CategoryID == _categoryID &&
                            x.Arena != null &&
                            x.Arena.IsSetupCompleted)
                        .ToListAsync();


                // =================================================
                // NO DATABASE RESULTS
                // =================================================

                if (items.Count == 0)
                {
                    _arenas.Clear();

                    ApplyFiltersAndSorting();

                    return;
                }


                // =================================================
                // GROUP UNITS BY ARENA
                // =================================================

                var arenaGroups =
                    items
                        .Where(x => x.Arena != null)
                        .GroupBy(x => x.ArenaID)
                        .Select(group =>
                        {
                            // -------------------------------------------------
                            // Get arena
                            // -------------------------------------------------

                            Arena arena =
                                group.First().Arena!;


                            // -------------------------------------------------
                            // TOTAL UNITS
                            // -------------------------------------------------

                            int totalUnitCount =
                                group.Count();


                            // -------------------------------------------------
                            // AVAILABLE UNITS
                            // -------------------------------------------------

                            int availableUnitCount =
                                group.Count(
                                    x => x.IsAvailable);


                            // -------------------------------------------------
                            // PRICE
                            // -------------------------------------------------

                            decimal minimumPrice =
                                group.Any()
                                    ? group.Min(
                                        x => x.PricePerHour)
                                    : 0;


                            // -------------------------------------------------
                            // CREATE CUSTOMER ARENA CARD
                            // -------------------------------------------------

                            return new ArenaItemModel
                            {
                                // =========================================
                                // ARENA
                                // =========================================

                                ArenaID =
                                    arena.ArenaID,

                                ArenaName =
                                    arena.ArenaName ??
                                    string.Empty,

                                Location =
                                    arena.Location ??
                                    string.Empty,


                                // =========================================
                                // IMAGE
                                // =========================================

                                CoverImageUrl =
                                    GetArenaImage(
                                        arena.CoverImageUrl),


                                // =========================================
                                // SPORT
                                // =========================================

                                CategoryID =
                                    _categoryID,

                                SportName =
                                    _categoryName,


                                // =========================================
                                // UNITS
                                // =========================================

                                TotalUnits =
                                    totalUnitCount,

                                AvailableUnits =
                                    availableUnitCount,


                                // =========================================
                                // PRICE
                                // =========================================

                                StartingPrice =
                                    minimumPrice,


                                // =========================================
                                // DEMO RATING
                                // =========================================

                                Rating =
                                    GetDemoRating(
                                        arena.ArenaID),


                                // =========================================
                                // DEMO DISTANCE
                                // =========================================

                                DistanceKm =
                                    GetDemoDistance(
                                        arena.ArenaID),


                                // =========================================
                                // OPEN / CLOSED
                                // =========================================

                                OpenStatus =
                                    IsArenaOpen(arena)
                                        ? "Open now"
                                        : "Closed"
                            };
                        })
                        .OrderBy(
                            x => x.ArenaName)
                        .ToList();


                // =================================================
                // UPDATE MASTER COLLECTION
                // =================================================

                _arenas.Clear();

                foreach (
                    ArenaItemModel arena
                    in arenaGroups)
                {
                    _arenas.Add(arena);
                }


                // =================================================
                // APPLY SEARCH / FILTERS / SORT
                // =================================================

                ApplyFiltersAndSorting();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Arena Discovery Error",
                    $"Unable to load arenas.\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                _isLoading = false;
            }
        }


        // =========================================================
        // DEMO RATING
        // =========================================================
        //
        // Training/demo data.
        // Same ArenaID = same rating.
        //
        // =========================================================

        private static double GetDemoRating(
            int arenaID)
        {
            double[] ratings =
            {
                4.7,
                4.5,
                4.3,
                4.8,
                4.1,
                4.6
            };

            return ratings[
                Math.Abs(arenaID) %
                ratings.Length];
        }


        // =========================================================
        // DEMO DISTANCE
        // =========================================================
        //
        // Training/demo data.
        // =========================================================

        private static double GetDemoDistance(
            int arenaID)
        {
            double[] distances =
            {
                1.2,
                2.4,
                3.1,
                4.6,
                5.3,
                6.8
            };

            return distances[
                Math.Abs(arenaID) %
                distances.Length];
        }


        // =========================================================
        // ARENA IMAGE
        // =========================================================

        private static string GetArenaImage(
            string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return "arena_bg.jpg";
            }

            return imageUrl.Trim();
        }


        // =========================================================
        // CHECK ARENA OPEN / CLOSED
        // =========================================================

        private static bool IsArenaOpen(
            Arena arena)
        {
            TimeSpan now =
                DateTime.Now.TimeOfDay;

            TimeSpan opening =
                arena.OpeningTime;

            TimeSpan closing =
                arena.ClosingTime;


            // =====================================================
            // NORMAL HOURS
            // Example:
            //
            // 08:00 → 23:00
            // =====================================================

            if (opening <= closing)
            {
                return
                    now >= opening &&
                    now <= closing;
            }


            // =====================================================
            // OVERNIGHT HOURS
            // Example:
            //
            // 18:00 → 02:00
            // =====================================================

            return
                now >= opening ||
                now <= closing;
        }


        // =========================================================
        // SEARCH TEXT CHANGED
        // =========================================================

        private void OnSearchTextChanged(
            object? sender,
            TextChangedEventArgs e)
        {
            ApplyFiltersAndSorting();
        }


        // =========================================================
        // APPLY SEARCH + FILTERS + SORT
        // =========================================================

        private void ApplyFiltersAndSorting()
        {
            IEnumerable<ArenaItemModel> result =
                _arenas;


            // =====================================================
            // SEARCH
            // =====================================================

            string search =
                TxtSearch?.Text?
                    .Trim() ??
                string.Empty;


            if (!string.IsNullOrWhiteSpace(search))
            {
                result =
                    result.Where(
                        arena =>
                            (
                                arena.ArenaName ??
                                string.Empty
                            )
                            .Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase)

                            ||

                            (
                                arena.Location ??
                                string.Empty
                            )
                            .Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase));
            }


            // =====================================================
            // AVAILABLE ONLY
            // =====================================================

            if (_availableOnly)
            {
                result =
                    result.Where(
                        arena =>
                            arena.AvailableUnits > 0);
            }


            // =====================================================
            // OPEN NOW ONLY
            // =====================================================

            if (_openNowOnly)
            {
                result =
                    result.Where(
                        arena =>
                            string.Equals(
                                arena.OpenStatus,
                                "Open now",
                                StringComparison.OrdinalIgnoreCase));
            }


            // =====================================================
            // MAXIMUM PRICE
            // =====================================================

            if (_maximumPrice.HasValue)
            {
                result =
                    result.Where(
                        arena =>
                            arena.StartingPrice <=
                            _maximumPrice.Value);
            }


            // =====================================================
            // SORTING
            // =====================================================

            result =
                _sortOption switch
                {
                    "Nearest" =>
                        result.OrderBy(
                            arena =>
                                arena.DistanceKm),

                    "Cheapest" =>
                        result.OrderBy(
                            arena =>
                                arena.StartingPrice),

                    "Highest Rated" =>
                        result.OrderByDescending(
                            arena =>
                                arena.Rating),

                    _ =>
                        result
                            .OrderByDescending(
                                arena =>
                                    arena.Rating)
                            .ThenBy(
                                arena =>
                                    arena.DistanceKm)
                };


            // =====================================================
            // CREATE FILTERED LIST
            // =====================================================

            _filteredArenas =
                result.ToList();


            // =====================================================
            // UPDATE COLLECTION VIEW
            // =====================================================

            CvArenas.ItemsSource =
                _filteredArenas;


            // =====================================================
            // UPDATE ARENA COUNT
            // =====================================================

            LblArenaCount.Text =
                $"{_filteredArenas.Count} " +
                $"{(
                    _filteredArenas.Count == 1
                        ? "arena"
                        : "arenas"
                )} available";


            // =====================================================
            // EMPTY STATE
            // =====================================================

            bool hasResults =
                _filteredArenas.Count > 0;


            EmptyState.IsVisible =
                !hasResults;

            CvArenas.IsVisible =
                hasResults;


            if (!hasResults)
            {
                LblEmptyMessage.Text =
                    string.IsNullOrWhiteSpace(search)
                        ? "No arenas match the selected filters."
                        : $"No arenas found matching \"{search}\".";
            }
        }


        // =========================================================
        // FILTER BUTTON
        // =========================================================

        private async void OnFiltersTapped(
            object? sender,
            TappedEventArgs e)
        {
            await DisplayAlertAsync(
                "Filters",
                "Use the Available and Open now filters below.",
                "OK");
        }


        // =========================================================
        // AVAILABLE FILTER
        // =========================================================

        private void OnAvailableFilterTapped(
            object? sender,
            TappedEventArgs e)
        {
            _availableOnly =
                !_availableOnly;


            AvailableFilterChip.BackgroundColor =
                _availableOnly
                    ? Color.FromArgb("#12302D")
                    : Color.FromArgb("#191B20");


            ApplyFiltersAndSorting();
        }


        // =========================================================
        // OPEN NOW FILTER
        // =========================================================

        private void OnOpenNowFilterTapped(
            object? sender,
            TappedEventArgs e)
        {
            _openNowOnly =
                !_openNowOnly;


            OpenNowFilterChip.BackgroundColor =
                _openNowOnly
                    ? Color.FromArgb("#12302D")
                    : Color.FromArgb("#191B20");


            ApplyFiltersAndSorting();
        }


        // =========================================================
        // SORT
        // =========================================================

        private void OnSortChanged(
            object? sender,
            EventArgs e)
        {
            if (SortPicker.SelectedIndex < 0)
                return;


            _sortOption =
                SortPicker.SelectedItem?
                    .ToString() ??
                "Recommended";


            ApplyFiltersAndSorting();
        }


        // =========================================================
        // PUBLIC FILTER METHODS
        // =========================================================

        public void SetAvailableFilter(
            bool enabled)
        {
            _availableOnly =
                enabled;

            ApplyFiltersAndSorting();
        }


        public void SetOpenNowFilter(
            bool enabled)
        {
            _openNowOnly =
                enabled;

            ApplyFiltersAndSorting();
        }


        public void SetMaximumPrice(
            decimal? maximumPrice)
        {
            _maximumPrice =
                maximumPrice;

            ApplyFiltersAndSorting();
        }


        public void SetSortOption(
            string option)
        {
            if (string.IsNullOrWhiteSpace(option))
                return;


            _sortOption =
                option;


            ApplyFiltersAndSorting();
        }


        // =========================================================
        // ARENA CARD TAP
        // =========================================================

        private async void OnArenaTapped(
     object? sender,
     TappedEventArgs e)
        {
            try
            {
                if (sender is not Grid grid)
                    return;

                if (grid.BindingContext
                    is not ArenaItemModel arena)
                {
                    return;
                }

                if (arena.ArenaID <= 0)
                {
                    await DisplayAlertAsync(
                        "Arena",
                        "Invalid arena selected.",
                        "OK");

                    return;
                }

                if (arena.CategoryID <= 0)
                {
                    await DisplayAlertAsync(
                        "Sport",
                        "Invalid sport selected.",
                        "OK");

                    return;
                }

                await Shell.Current.GoToAsync(
                    "CustomerArenaDetailsPage",
                    new Dictionary<string, object>
                    {
                        ["ArenaID"] = arena.ArenaID,
                        ["CategoryID"] = arena.CategoryID
                    });
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Arena Selection Error",
                    $"Unable to open this arena.\n\n{ex.Message}",
                    "OK");
            }
        }

        // =========================================================
        // BACK BUTTON
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


        // =========================================================
        // EMPTY STATE
        // =========================================================

        private void ShowEmptyState(
            string message)
        {
            LblEmptyMessage.Text =
                message;

            EmptyState.IsVisible =
                true;

            CvArenas.IsVisible =
                false;

            LblArenaCount.Text =
                "0 arenas available";
        }
    }
}
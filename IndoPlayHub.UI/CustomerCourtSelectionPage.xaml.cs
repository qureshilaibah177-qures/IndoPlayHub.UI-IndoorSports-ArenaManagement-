using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using Microsoft.EntityFrameworkCore;

namespace IndoPlayHub.UI
{
    public partial class CustomerCourtSelectionPage :
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

        private bool _isAllSportsView;


        // =========================================================
        // STATE
        // =========================================================

        private bool _isLoading;

        private bool _queryReceived;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public CustomerCourtSelectionPage(
            AppDbContext dbContext)
        {
            InitializeComponent();

            _dbContext = dbContext;
        }


        // =========================================================
        // QUERY PARAMETERS
        // =========================================================

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            try
            {
                // =================================================
                // ARENA
                // =================================================

                _arenaID =
                    GetIntQueryValue(
                        query,
                        "ArenaID");


                // =================================================
                // CATEGORY
                // =================================================

                _categoryID =
                    GetIntQueryValue(
                        query,
                        "CategoryID");


                // =================================================
                // MODE
                // =================================================

                _isAllSportsView =
                    _categoryID <= 0;


                // =================================================
                // PREVENT DUPLICATE LOAD
                // =================================================

                if (_queryReceived)
                    return;

                _queryReceived = true;


                // =================================================
                // LOAD
                // =================================================

                _ = LoadCourtsAsync();
            }
            catch (Exception ex)
            {
                _ = ShowErrorAsync(
                    "Loading Error",
                    $"Unable to open court selection.\n\n{ex.Message}");
            }
        }


        // =========================================================
        // QUERY HELPER
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
        // LOAD COURTS
        // =========================================================

        private async Task LoadCourtsAsync()
        {
            if (_isLoading)
                return;

            _isLoading = true;

            try
            {
                ShowLoadingState();


                // =================================================
                // VALIDATE ARENA
                // =================================================

                if (_arenaID <= 0)
                {
                    await ShowErrorAsync(
                        "Arena",
                        "Invalid arena selected.");

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
                // LOAD SELECTED CATEGORY
                // =================================================

                Category? selectedCategory = null;

                if (!_isAllSportsView)
                {
                    selectedCategory =
                        await _dbContext.Categories
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                x =>
                                    x.CategoryID ==
                                    _categoryID);


                    if (selectedCategory == null)
                    {
                        await ShowErrorAsync(
                            "Sport",
                            "The selected sport could not be found.");

                        return;
                    }
                }


                // =================================================
                // ARENA INFORMATION
                // =================================================

                LblArenaName.Text =
                    string.IsNullOrWhiteSpace(
                        arena.ArenaName)
                        ? "Arena"
                        : arena.ArenaName;

                LblArenaLocation.Text =
                    string.IsNullOrWhiteSpace(
                        arena.Location)
                        ? "📍 Location unavailable"
                        : $"📍 {arena.Location}";


                // =================================================
                // SPORT INFORMATION
                // =================================================

                if (_isAllSportsView)
                {
                    LblSelectedSport.Text =
                        "🏟️ All Sports";

                    LblSubheading.Text =
                        "Choose a court from the sports available at this arena.";

                    LblPageTitle.Text =
                        "Select a Court";

                    LblSportIcon.Text =
                        "🏟️";
                }
                else
                {
                    string categoryName =
                        string.IsNullOrWhiteSpace(
                            selectedCategory?.CategoryName)
                            ? "Selected Sport"
                            : selectedCategory!.CategoryName.Trim();

                    LblSelectedSport.Text =
                        $"🏟️ {categoryName}";

                    LblSubheading.Text =
                        $"Choose a {categoryName} court for your booking.";

                    LblPageTitle.Text =
                        $"{categoryName} Courts";

                    LblSportIcon.Text =
                        GetSportIcon(categoryName);
                }


                // =================================================
                // DATABASE QUERY
                // =================================================

                IQueryable<CourtOrMenuItem> query =
                    _dbContext.CourtsAndMenuItems
                        .AsNoTracking()
                        .Include(x => x.Category)
                        .Where(
                            x =>
                                x.ArenaID ==
                                _arenaID);


                // =================================================
                // SPORT FILTER
                // =================================================

                if (!_isAllSportsView)
                {
                    query =
                        query.Where(
                            x =>
                                x.CategoryID ==
                                _categoryID);
                }


                // =================================================
                // ORDER
                // =================================================

                List<CourtOrMenuItem> databaseItems =
                    await query
                        .OrderBy(
                            x =>
                                x.CategoryID)
                        .ThenBy(
                            x =>
                                x.ItemName)
                        .ToListAsync();


                // =================================================
                // CUSTOMER COURT LIST
                // =================================================

                List<CustomerCourtSelectionItem> courts =
                    new List<CustomerCourtSelectionItem>();


                // =================================================
                // CONVERT DATABASE ITEMS
                // =================================================

                foreach (
                    CourtOrMenuItem item
                    in databaseItems)
                {
                    string categoryName =
                        item.Category?.CategoryName
                        ?? selectedCategory?.CategoryName
                        ?? "Sport";


                    // =================================================
                    // IMPORTANT IMAGE LOGIC
                    // =================================================
                    //
                    // DO NOT use:
                    //
                    // item.ImageUrl
                    //
                    // The customer's court image is determined
                    // strictly by CategoryID.
                    //
                    // This prevents an incorrectly stored database
                    // ImageUrl from showing the wrong sport picture.
                    // =================================================

                    string courtImage =
                        GetCourtImage(
                            item.CategoryID,
                            categoryName);


                    courts.Add(
                        new CustomerCourtSelectionItem
                        {
                            ItemID =
                                item.ItemID,

                            ArenaID =
                                item.ArenaID,

                            CategoryID =
                                item.CategoryID,

                            ItemName =
                                string.IsNullOrWhiteSpace(
                                    item.ItemName)
                                    ? "Court"
                                    : item.ItemName.Trim(),

                            PricePerHour =
                                item.PricePerHour,

                            IsAvailable =
                                item.IsAvailable,

                            ImageUrl =
                                courtImage,

                            CategoryName =
                                categoryName.Trim()
                        });
                }


                // =================================================
                // DISPLAY COURTS
                // =================================================
                //
                // IMPORTANT:
                // This MUST be outside the foreach loop.
                // =================================================

                CvCourts.ItemsSource =
                    courts;


                // =================================================
                // EMPTY STATE
                // =================================================

                if (courts.Count == 0)
                {
                    CvCourts.IsVisible = false;

                    EmptyStateLayout.IsVisible = true;

                    LblEmptyMessage.Text =
                        _isAllSportsView
                            ? "This arena currently has no courts configured."
                            : "This arena currently has no courts configured for the selected sport.";
                }
                else
                {
                    CvCourts.IsVisible = true;

                    EmptyStateLayout.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(
                    "Court Selection Error",
                    $"Unable to load courts.\n\n{ex.Message}");
            }
            finally
            {
                HideLoadingState();

                _isLoading = false;
            }
        }


        // =========================================================
        // COURT IMAGE
        // =========================================================
        //
        // IMPORTANT:
        // CategoryID is the PRIMARY source.
        //
        // CategoryName is only a fallback.
        //
        // Database ImageUrl is NEVER used.
        // =========================================================

        private static string GetCourtImage(
            int categoryID,
            string? categoryName)
        {
            // =====================================================
            // CATEGORY-ID MAPPING
            // =====================================================
            //
            // Based on your IndoPlayHub Categories seed structure:
            //
            // 1  = Indoor Futsal
            // 2  = Indoor Cricket
            // 3  = Badminton
            // 4  = Table Tennis
            // 5  = Basketball
            // 6  = Squash
            // 7  = Indoor Volleyball
            // 8  = Billiards & Pool
            // 9  = Indoor Boxing
            // 10 = Fitness & Gym
            // 11 = Snooker Room
            // 12 = Yoga & Pilates
            // 13 = Indoor Board Games
            // 14 = Indoor Climbing
            // 15 = Cricket
            // 16 = Martial Arts Dojo
            //
            // Additional categories such as Football/Soccer are
            // handled by the name fallback below.
            // =====================================================

            return categoryID switch
            {
                1 =>
                    "futsal.jpg",

                2 =>
                    "cricket.jpg",

                3 =>
                    "badminton.jpg",

                4 =>
                    "tabletennis.jpg",

                5 =>
                    "basketball.jpg",

                6 =>
                    "squash.jpg",

                7 =>
                    "volleyball.jpg",

                8 =>
                    "billiardsandpool.jpg",

                9 =>
                    "boxing.jpg",

                10 =>
                    "fitnessandgym.jpg",

                11 =>
                    "snooker.jpg",

                12 =>
                    "yogaandpilates.jpg",

                13 =>
                    "boardgames.jpg",

                14 =>
                    "climbing.jpg",

                15 =>
                    "cricket.jpg",

                16 =>
                    "martialartsdojo.jpg",

                _ =>
                    GetCourtImageByName(
                        categoryName)
            };
        }


        // =========================================================
        // IMAGE FALLBACK BY CATEGORY NAME
        // =========================================================

        private static string GetCourtImageByName(
            string? categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return "arena_bg.jpg";


            string name =
                categoryName
                    .Trim()
                    .ToLowerInvariant();


            // =====================================================
            // FUTSAL
            // =====================================================

            if (name.Contains("futsal"))
                return "futsal.jpg";


            // =====================================================
            // SOCCER
            // =====================================================

            if (name.Contains("soccer"))
                return "indoorsoccer.jpg";


            // =====================================================
            // FOOTBALL
            // =====================================================

            if (name.Contains("football"))
                return "football.jpg";


            // =====================================================
            // CRICKET
            // =====================================================

            if (name.Contains("cricket"))
                return "cricket.jpg";


            // =====================================================
            // BADMINTON
            // =====================================================

            if (name.Contains("badminton"))
                return "badminton.jpg";


            // =====================================================
            // TABLE TENNIS
            // =====================================================

            if (name.Contains("table tennis"))
                return "tabletennis.jpg";


            // =====================================================
            // BASKETBALL
            // =====================================================

            if (name.Contains("basketball"))
                return "basketball.jpg";


            // =====================================================
            // SQUASH
            // =====================================================

            if (name.Contains("squash"))
                return "squash.jpg";


            // =====================================================
            // VOLLEYBALL
            // =====================================================

            if (name.Contains("volleyball"))
                return "volleyball.jpg";


            // =====================================================
            // BILLIARDS
            // =====================================================

            if (name.Contains("billiards"))
                return "billiardsandpool.jpg";


            if (name.Contains("pool"))
                return "billiardsandpool.jpg";


            // =====================================================
            // BOXING
            // =====================================================

            if (name.Contains("boxing"))
                return "boxing.jpg";


            // =====================================================
            // FITNESS
            // =====================================================

            if (name.Contains("fitness"))
                return "fitnessandgym.jpg";


            if (name.Contains("gym"))
                return "fitnessandgym.jpg";


            // =====================================================
            // SNOOKER
            // =====================================================

            if (name.Contains("snooker"))
                return "snooker.jpg";


            // =====================================================
            // YOGA
            // =====================================================

            if (name.Contains("yoga"))
                return "yogaandpilates.jpg";


            if (name.Contains("pilates"))
                return "yogaandpilates.jpg";


            // =====================================================
            // BOARD GAMES
            // =====================================================

            if (name.Contains("board"))
                return "boardgames.jpg";


            // =====================================================
            // CLIMBING
            // =====================================================

            if (name.Contains("climb"))
                return "climbing.jpg";


            // =====================================================
            // MARTIAL ARTS
            // =====================================================

            if (name.Contains("martial"))
                return "martialartsdojo.jpg";


            // =====================================================
            // DEFAULT
            // =====================================================

            return "arena_bg.jpg";
        }


        // =========================================================
        // SPORT ICON
        // =========================================================

        private static string GetSportIcon(
            string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return "🏟️";


            string name =
                categoryName
                    .Trim()
                    .ToLowerInvariant();


            if (name.Contains("basket"))
                return "🏀";

            if (name.Contains("futsal"))
                return "⚽";

            if (name.Contains("soccer"))
                return "⚽";

            if (name.Contains("football"))
                return "🏈";

            if (name.Contains("cricket"))
                return "🏏";

            if (name.Contains("badminton"))
                return "🏸";

            if (name.Contains("tennis"))
                return "🎾";

            if (name.Contains("padel"))
                return "🎾";

            if (name.Contains("volley"))
                return "🏐";

            if (name.Contains("table"))
                return "🏓";

            if (name.Contains("climb"))
                return "🧗";

            if (name.Contains("squash"))
                return "🏸";

            if (name.Contains("boxing"))
                return "🥊";

            if (name.Contains("snooker"))
                return "🎱";

            if (name.Contains("billiard"))
                return "🎱";

            if (name.Contains("yoga"))
                return "🧘";

            if (name.Contains("martial"))
                return "🥋";

            return "🏟️";
        }


        // =========================================================
        // COURT SELECTED
        // =========================================================

        private async void OnCourtSelectedClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                // =================================================
                // VALIDATE BUTTON
                // =================================================

                if (sender is not Button button)
                    return;


                // =================================================
                // GET COURT
                // =================================================

                if (button.CommandParameter
                    is not CustomerCourtSelectionItem court)
                {
                    await ShowErrorAsync(
                        "Court",
                        "Unable to identify the selected court.");

                    return;
                }


                // =================================================
                // AVAILABILITY
                // =================================================

                if (!court.IsAvailable)
                {
                    await DisplayAlertAsync(
                        "Court Unavailable",
                        "This court is currently unavailable.",
                        "OK");

                    return;
                }


                // =================================================
                // ARENA VALIDATION
                // =================================================

                if (court.ArenaID <= 0)
                {
                    await ShowErrorAsync(
                        "Arena",
                        "The selected arena could not be identified.");

                    return;
                }


                // =================================================
                // COURT VALIDATION
                // =================================================

                if (court.ItemID <= 0)
                {
                    await ShowErrorAsync(
                        "Court",
                        "The selected court could not be identified.");

                    return;
                }


                // =================================================
                // CATEGORY VALIDATION
                // =================================================

                if (court.CategoryID <= 0)
                {
                    await ShowErrorAsync(
                        "Sport",
                        "The selected sport could not be identified.");

                    return;
                }


                // =================================================
                // FINAL DATABASE VALIDATION
                // =================================================

                CourtOrMenuItem? databaseCourt =
                    await _dbContext.CourtsAndMenuItems
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x =>
                                x.ItemID == court.ItemID &&
                                x.ArenaID == court.ArenaID &&
                                x.CategoryID == court.CategoryID);


                if (databaseCourt == null)
                {
                    await ShowErrorAsync(
                        "Court",
                        "This court is no longer available.");

                    return;
                }


                // =================================================
                // CHECK CURRENT AVAILABILITY AGAIN
                // =================================================

                if (!databaseCourt.IsAvailable)
                {
                    await DisplayAlertAsync(
                        "Court Unavailable",
                        "This court has just become unavailable.",
                        "OK");

                    return;
                }


                // =================================================
                // PHASE 7 PARAMETERS
                // =================================================

                Dictionary<string, object>
                    navigationParameters =
                        new Dictionary<string, object>
                        {
                            ["ArenaID"] =
                                databaseCourt.ArenaID,

                            ["CategoryID"] =
                                databaseCourt.CategoryID,

                            ["ItemID"] =
                                databaseCourt.ItemID
                        };


                // =================================================
                // NAVIGATE
                // =================================================

                await Shell.Current.GoToAsync(
                    nameof(CustomerDateTimeSelectionPage),
                    navigationParameters);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(
                    "Selection Error",
                    $"Unable to continue with this court.\n\n{ex.Message}");
            }
        }


        // =========================================================
        // LOADING
        // =========================================================

        private void ShowLoadingState()
        {
            LoadingLayout.IsVisible = true;

            LoadingIndicator.IsRunning = true;

            CvCourts.IsVisible = false;

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
                // Prevent secondary UI errors.
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
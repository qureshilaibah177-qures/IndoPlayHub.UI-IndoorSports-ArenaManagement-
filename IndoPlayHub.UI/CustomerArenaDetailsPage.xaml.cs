using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Controls.Shapes;

namespace IndoPlayHub.UI
{
    public partial class CustomerArenaDetailsPage :
        ContentPage,
        IQueryAttributable
    {
        // =========================================================
        // DATABASE
        // =========================================================

        private readonly AppDbContext _dbContext;


        // =========================================================
        // SELECTED ARENA / SPORT
        // =========================================================

        private int _arenaID;

        private int _categoryID;

        // TRUE when customer entered through:
        // Find Arenas -> City -> View Arena
        //
        // FALSE when customer entered through:
        // Sport -> Arena -> View Arena
        private bool _isAllSportsView;


        // =========================================================
        // LOADING STATE
        // =========================================================

        private bool _isLoading;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public CustomerArenaDetailsPage(
            AppDbContext dbContext)
        {
            InitializeComponent();

            _dbContext = dbContext;
        }


        // =========================================================
        // RECEIVE QUERY PARAMETERS
        // =========================================================

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            try
            {
                // =================================================
                // ARENA ID
                // =================================================

                if (query.TryGetValue(
                    "ArenaID",
                    out object? arenaID))
                {
                    if (arenaID is int id)
                    {
                        _arenaID = id;
                    }
                    else if (
                        int.TryParse(
                            arenaID?.ToString(),
                            out int parsedID))
                    {
                        _arenaID = parsedID;
                    }
                }


                // =================================================
                // CATEGORY ID
                // =================================================
                //
                // CategoryID is optional.
                //
                // If supplied:
                //     Sport -> Arena -> View Arena
                //
                // If NOT supplied:
                //     Find Arenas -> City -> View Arena
                //     -> show ALL sports.
                // =================================================

                _categoryID = 0;

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
                // DETERMINE DISPLAY MODE
                // =================================================

                _isAllSportsView =
                    _categoryID <= 0;


                // =================================================
                // LOAD DETAILS
                // =================================================

                _ = LoadArenaDetailsAsync();
            }
            catch (Exception ex)
            {
                _ = DisplayAlertAsync(
                    "Loading Error",
                    $"Unable to open arena details.\n\n{ex.Message}",
                    "OK");
            }
        }


        // =========================================================
        // LOAD ARENA DETAILS
        // =========================================================

        private async Task LoadArenaDetailsAsync()
        {
            if (_isLoading)
                return;

            _isLoading = true;

            try
            {
                // =================================================
                // VALIDATE ARENA
                // =================================================

                if (_arenaID <= 0)
                {
                    await DisplayAlertAsync(
                        "Arena",
                        "Invalid arena selected.",
                        "OK");

                    return;
                }


                // =================================================
                // LOAD ARENA
                // =================================================

                Arena? arena =
                    await _dbContext.Arenas
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            a => a.ArenaID == _arenaID);


                if (arena == null)
                {
                    await DisplayAlertAsync(
                        "Arena",
                        "Arena could not be found.",
                        "OK");

                    return;
                }


                // =================================================
                // SELECTED CATEGORY
                // =================================================

                Category? category = null;


                // =================================================
                // SPORT MODE
                // =================================================
                //
                // Only verify CategoryID when customer came from
                // a sport card.
                // =================================================

                if (!_isAllSportsView)
                {
                    category =
                        await _dbContext.Categories
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                c => c.CategoryID == _categoryID);


                    if (category == null)
                    {
                        await DisplayAlertAsync(
                            "Sport",
                            "The selected sport could not be found.",
                            "OK");

                        return;
                    }
                }


                // =================================================
                // CREATE CUSTOMER DETAILS MODEL
                // =================================================

                CustomerArenaDetails details =
                    new CustomerArenaDetails
                    {
                        // =========================================
                        // ARENA
                        // =========================================

                        ArenaID =
                            arena.ArenaID,

                        ArenaName =
                            arena.ArenaName,

                        Location =
                            arena.Location,

                        CoverImageUrl =
                            GetArenaImage(
                                arena.CoverImageUrl),


                        // =========================================
                        // CONTACT
                        // =========================================

                        PhoneNumber =
                            arena.PhoneNumber,

                        Email =
                            arena.Email,


                        // =========================================
                        // HOURS
                        // =========================================

                        OpeningTime =
                            arena.OpeningTime,

                        ClosingTime =
                            arena.ClosingTime,


                        // =========================================
                        // RATING
                        // =========================================

                        Rating =
                            GetDemoRating(
                                arena.ArenaID),

                        ReviewCount =
                            GetDemoReviewCount(
                                arena.ArenaID),


                        // =========================================
                        // DESCRIPTION
                        // =========================================

                        Description =
                            GetDemoDescription(
                                arena.ArenaName)
                    };


                // =================================================
                // LOAD COURTS / SPORTS
                // =================================================
                //
                // SPORT MODE:
                //
                // ArenaID + CategoryID
                //
                // ALL SPORTS MODE:
                //
                // ArenaID only
                //
                // =================================================

                IQueryable<CourtOrMenuItem> itemsQuery =
                    _dbContext.CourtsAndMenuItems
                        .AsNoTracking()
                        .Include(x => x.Category)
                        .Where(x =>
                            x.ArenaID == arena.ArenaID);


                if (!_isAllSportsView)
                {
                    itemsQuery =
                        itemsQuery.Where(x =>
                            x.CategoryID == _categoryID);
                }


                List<CourtOrMenuItem> items =
                    await itemsQuery
                        .OrderBy(x => x.CategoryID)
                        .ThenBy(x => x.ItemName)
                        .ToListAsync();


                // =================================================
                // CONVERT DATABASE ITEMS
                // =================================================

                foreach (
                    CourtOrMenuItem item in items)
                {
                    string categoryName =
                        item.Category?.CategoryName
                        ?? category?.CategoryName
                        ?? "Sport";


                    details.Courts.Add(
                        new CustomerArenaCourtItem
                        {
                            ItemID =
                                item.ItemID,

                            ItemName =
                                item.ItemName,

                            CategoryName =
                                categoryName,

                            PricePerHour =
                                item.PricePerHour,

                            IsAvailable =
                                item.IsAvailable
                        });
                }


                // =================================================
                // DEMO FACILITIES
                // =================================================

                LoadDemoFacilities(details);


                // =================================================
                // DISPLAY
                // =================================================

                DisplayArenaDetails(details);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Arena Details Error",
                    $"Unable to load arena details.\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                _isLoading = false;
            }
        }


        // =========================================================
        // DISPLAY ARENA DETAILS
        // =========================================================

        private void DisplayArenaDetails(
            CustomerArenaDetails details)
        {
            // =====================================================
            // HERO
            // =====================================================

            ImgArena.Source =
                details.CoverImageUrl;

            LblArenaNameHero.Text =
                details.ArenaName;

            LblHeroLocation.Text =
                $"📍 {details.Location}";


            // =====================================================
            // SUMMARY
            // =====================================================

            LblArenaName.Text =
                details.ArenaName;

            LblRating.Text =
                details.RatingText;

            LblReviews.Text =
                details.ReviewText;

            LblLocation.Text =
                $"📍 {details.Location}";

            LblOpeningHours.Text =
                $"🕐 {details.OpeningHoursText}";


            // =====================================================
            // ABOUT
            // =====================================================

            LblDescription.Text =
                details.Description;


            // =====================================================
            // COURTS
            // =====================================================

            CvCourts.ItemsSource =
                details.Courts;

            LblCourtCount.Text =
                details.Courts.Count == 1
                    ? "1 court"
                    : $"{details.Courts.Count} courts";

            LblSportsSubheading.Text =
                _isAllSportsView
                    ? "All sports and current hourly prices"
                    : "Courts and current hourly prices";


            // =====================================================
            // FACILITIES
            // =====================================================

            DisplayFacilities(
                details.Facilities);


            // =====================================================
            // CONTACT
            // =====================================================

            LblPhone.Text =
                string.IsNullOrWhiteSpace(
                    details.PhoneNumber)
                    ? "📞 Phone unavailable"
                    : $"📞 {details.PhoneNumber}";

            LblEmail.Text =
                string.IsNullOrWhiteSpace(
                    details.Email)
                    ? "✉️ Email unavailable"
                    : $"✉️ {details.Email}";


            // =====================================================
            // REVIEW SUMMARY
            // =====================================================

            LblReviewSummary.Text =
                $"{details.RatingText} • {details.ReviewText}";
        }


        // =========================================================
        // FACILITIES
        // =========================================================

        private void DisplayFacilities(
            List<string> facilities)
        {
            FacilitiesLayout.Children.Clear();

            foreach (string facility in facilities)
            {
                Border facilityBorder =
                    new Border
                    {
                        BackgroundColor =
                            Color.FromArgb("#191B20"),

                        Stroke =
                            Color.FromArgb("#292D35"),

                        StrokeThickness = 1,

                        Padding =
                            new Thickness(
                                12,
                                8),

                        Margin =
                            new Thickness(
                                0,
                                0,
                                8,
                                8),

                        Content =
                            new Label
                            {
                                Text =
                                    $"✓  {facility}",

                                FontSize = 12,

                                TextColor =
                                    Color.FromArgb("#D0D3D8"),

                                VerticalOptions =
                                    LayoutOptions.Center
                            }
                    };

                facilityBorder.StrokeShape =
                    new RoundRectangle
                    {
                        CornerRadius = 14
                    };

                FacilitiesLayout.Children.Add(
                    facilityBorder);
            }
        }


        // =========================================================
        // ARENA IMAGE
        // =========================================================

        private static string GetArenaImage(
            string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return "arena_bg.jpg";

            return imageUrl.Trim();
        }


        // =========================================================
        // DEMO RATING
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
        // DEMO REVIEW COUNT
        // =========================================================

        private static int GetDemoReviewCount(
            int arenaID)
        {
            int[] counts =
            {
                124,
                98,
                76,
                151,
                62,
                113
            };

            return counts[
                Math.Abs(arenaID) %
                counts.Length];
        }


        // =========================================================
        // DEMO DESCRIPTION
        // =========================================================

        private static string GetDemoDescription(
            string arenaName)
        {
            return
                $"{arenaName} is a customer-friendly indoor sports " +
                "arena offering quality playing facilities, " +
                "comfortable surroundings and convenient hourly " +
                "court bookings. Availability and prices shown " +
                "above are based on the arena's current setup.";
        }


        // =========================================================
        // DEMO FACILITIES
        // =========================================================

        private static void LoadDemoFacilities(
            CustomerArenaDetails details)
        {
            string[] facilities =
            {
                "Parking",
                "Changing room",
                "Washroom",
                "Drinking water",
                "Seating",
                "Equipment rental",
                "Wi-Fi"
            };

            foreach (string facility in facilities)
            {
                details.Facilities.Add(
                    facility);
            }
        }


        // =========================================================
        // CALL
        // =========================================================

        private async void OnCallClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                if (_arenaID <= 0)
                {
                    await DisplayAlertAsync(
                        "Contact",
                        "Invalid arena selected.",
                        "OK");

                    return;
                }

                Arena? arena =
                    await _dbContext.Arenas
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            a => a.ArenaID == _arenaID);

                if (arena == null ||
                    string.IsNullOrWhiteSpace(
                        arena.PhoneNumber))
                {
                    await DisplayAlertAsync(
                        "Contact",
                        "Phone number is not available.",
                        "OK");

                    return;
                }

                string phone =
                    arena.PhoneNumber.Trim();

                bool canOpen =
                    await Launcher.Default
                        .CanOpenAsync(
                            $"tel:{phone}");

                if (canOpen)
                {
                    await Launcher.Default
                        .OpenAsync(
                            $"tel:{phone}");
                }
                else
                {
                    await DisplayAlertAsync(
                        "Phone Number",
                        phone,
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Contact Error",
                    $"Unable to open the phone application.\n\n{ex.Message}",
                    "OK");
            }
        }


        // =========================================================
        // DIRECTIONS
        // =========================================================

        private async void OnDirectionsClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                if (_arenaID <= 0)
                {
                    await DisplayAlertAsync(
                        "Directions",
                        "Invalid arena selected.",
                        "OK");

                    return;
                }

                Arena? arena =
                    await _dbContext.Arenas
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            a => a.ArenaID == _arenaID);

                if (arena == null ||
                    string.IsNullOrWhiteSpace(
                        arena.Location))
                {
                    await DisplayAlertAsync(
                        "Directions",
                        "Arena location is not available.",
                        "OK");

                    return;
                }

                string encodedLocation =
                    Uri.EscapeDataString(
                        arena.Location.Trim());

                string mapsUrl =
                    "https://www.google.com/maps/search/" +
                    $"?api=1&query={encodedLocation}";

                await Launcher.Default
                    .OpenAsync(mapsUrl);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Directions Error",
                    $"Unable to open directions.\n\n{ex.Message}",
                    "OK");
            }
        }


        // =========================================================
        // PHASE 6 — VIEW AVAILABLE COURTS
        // =========================================================
        //
        // OLD BEHAVIOUR:
        //     Show an availability popup.
        //
        // NEW BEHAVIOUR:
        //     Navigate to CustomerCourtSelectionPage.
        //
        // Flow:
        //
        // Phase 5
        // Arena Details
        //       ↓
        // Phase 6
        // Court Selection
        //       ↓
        // Phase 7
        // Date & Time Selection
        //       ↓
        // Phase 8
        // Booking Summary
        //       ↓
        // Phase 9
        // Payment Method
        //
        // =========================================================

        private async void OnViewCourtsClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                // =================================================
                // VALIDATE ARENA
                // =================================================

                if (_arenaID <= 0)
                {
                    await DisplayAlertAsync(
                        "Availability",
                        "Invalid arena selected.",
                        "OK");

                    return;
                }


                // =================================================
                // CHECK WHETHER ARENA STILL EXISTS
                // =================================================

                bool arenaExists =
                    await _dbContext.Arenas
                        .AsNoTracking()
                        .AnyAsync(
                            x => x.ArenaID == _arenaID);

                if (!arenaExists)
                {
                    await DisplayAlertAsync(
                        "Arena",
                        "The selected arena could not be found.",
                        "OK");

                    return;
                }


                // =================================================
                // CHECK AVAILABLE COURTS
                // =================================================
                //
                // This prevents the customer from navigating to
                // an empty court-selection screen.
                //
                // SPORT MODE:
                //     Arena + selected Category
                //
                // ALL SPORTS MODE:
                //     Arena only
                // =================================================

                IQueryable<CourtOrMenuItem> query =
                    _dbContext.CourtsAndMenuItems
                        .AsNoTracking()
                        .Where(x =>
                            x.ArenaID == _arenaID &&
                            x.IsAvailable);


                if (!_isAllSportsView)
                {
                    query =
                        query.Where(x =>
                            x.CategoryID == _categoryID);
                }


                bool hasAvailableCourts =
                    await query.AnyAsync();


                // =================================================
                // NO AVAILABLE COURTS
                // =================================================

                if (!hasAvailableCourts)
                {
                    string message =
                        _isAllSportsView
                            ? "There are currently no available courts at this arena."
                            : "There are currently no available courts for this sport at this arena.";

                    await DisplayAlertAsync(
                        "Availability",
                        message,
                        "OK");

                    return;
                }


                // =================================================
                // PHASE 6 NAVIGATION
                // =================================================
                //
                // Pass ArenaID.
                //
                // Pass CategoryID when the customer came through
                // a specific sport.
                //
                // CategoryID = 0 means ALL SPORTS mode.
                // =================================================

                Dictionary<string, object> navigationParameters =
                    new Dictionary<string, object>
                    {
                        ["ArenaID"] =
                            _arenaID,

                        ["CategoryID"] =
                            _categoryID
                    };


                await Shell.Current.GoToAsync(
                    "CustomerCourtSelectionPage",
                    navigationParameters);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Booking",
                    $"Unable to open court selection.\n\n{ex.Message}",
                    "OK");
            }
        }


        // =========================================================
        // PHASE 15 — FOOD COURT / MENU
        // =========================================================

        private async void OnFoodCourtClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                if (_arenaID <= 0)
                {
                    await DisplayAlertAsync(
                        "Food Court",
                        "Invalid arena selected.",
                        "OK");

                    return;
                }

                bool arenaExists =
                    await _dbContext.Arenas
                        .AsNoTracking()
                        .AnyAsync(
                            x => x.ArenaID == _arenaID);

                if (!arenaExists)
                {
                    await DisplayAlertAsync(
                        "Food Court",
                        "The selected arena could not be found.",
                        "OK");

                    return;
                }

                Dictionary<string, object> navigationParameters =
                    new Dictionary<string, object>
                    {
                        ["ArenaID"] = _arenaID
                    };

                await Shell.Current.GoToAsync(
                    "CustomerMenuPage",
                    navigationParameters);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Food Court Error",
                    $"Unable to open the food court.\n\n{ex.Message}",
                    "OK");
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
                await DisplayAlertAsync(
                    "Navigation Error",
                    $"Unable to go back.\n\n{ex.Message}",
                    "OK");
            }
        }
    }
}
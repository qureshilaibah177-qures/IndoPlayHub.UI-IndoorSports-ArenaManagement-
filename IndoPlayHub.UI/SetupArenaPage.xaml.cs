using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

namespace IndoPlayHub.UI
{
    public partial class SetupArenaPage : ContentPage
    {
        private readonly AppDbContext _dbContext;
        private readonly int _arenaId;

        private bool _isSaving;
        private bool _hasLoaded;

        private List<SportSetupItem> _sports = new();

        public SetupArenaPage(
            AppDbContext dbContext,
            int arenaId)
        {
            InitializeComponent();

            _dbContext = dbContext;
            _arenaId = arenaId;

            // =====================================================
            // DEFAULT BUSINESS HOURS
            // =====================================================

            TpOpening.Time = new TimeSpan(8, 0, 0);
            TpClosing.Time = new TimeSpan(23, 0, 0);
        }

        // =========================================================
        // PAGE APPEARING
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_hasLoaded)
                return;

            _hasLoaded = true;

            await LoadSetupDataAsync();
        }

        // =========================================================
        // LOAD EXISTING ARENA + SPORTS
        // =========================================================

        private async Task LoadSetupDataAsync()
        {
            try
            {
                if (_arenaId <= 0)
                {
                    await DisplayAlertAsync(
                        "Setup Error",
                        "Invalid arena account.",
                        "OK");

                    return;
                }

                var arena = await _dbContext.Arenas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        a => a.ArenaID == _arenaId);

                if (arena == null)
                {
                    await DisplayAlertAsync(
                        "Setup Error",
                        "The arena account could not be found.",
                        "OK");

                    return;
                }

                // =================================================
                // STEP 1
                // =================================================

                TxtArenaName.Text =
                    arena.ArenaName ?? string.Empty;

                TxtEmail.Text =
                    arena.Email ?? string.Empty;

                TxtPhone.Text =
                    arena.PhoneNumber ?? string.Empty;

                // =================================================
                // STEP 2
                // =================================================

                TxtLocation.Text =
                    arena.Location ?? string.Empty;

                // Arena.OpeningTime and ClosingTime are NON-NULLABLE
                // TimeSpan values.

                TpOpening.Time =
                    arena.OpeningTime;

                TpClosing.Time =
                    arena.ClosingTime;

                // =================================================
                // LOAD SPORTS
                // =================================================

                var categories = await _dbContext.Categories
                    .AsNoTracking()
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();

                _sports = new List<SportSetupItem>();

                foreach (var category in categories)
                {
                    var existingItems =
                        await _dbContext.CourtsAndMenuItems
                            .AsNoTracking()
                            .Where(c =>
                                c.ArenaID == _arenaId &&
                                c.CategoryID ==
                                category.CategoryID)
                            .ToListAsync();

                    // Only currently available units count
                    // toward the configured quantity.

                    int activeQuantity =
                        existingItems.Count(
                            c => c.IsAvailable);

                    decimal existingRate =
                        existingItems
                            .Select(c => c.PricePerHour)
                            .FirstOrDefault();

                    string categoryName =
                        category.CategoryName ??
                        string.Empty;

                    _sports.Add(
                        new SportSetupItem
                        {
                            CategoryID =
                                category.CategoryID,

                            CategoryName =
                                categoryName,

                            Description =
                                GetSportDescription(
                                    categoryName),

                            IsSelected =
                                activeQuantity > 0,

                            Quantity =
                                activeQuantity > 0
                                    ? activeQuantity.ToString()
                                    : string.Empty,

                            DefaultRate =
                                existingRate > 0
                                    ? existingRate
                                    : GetDefaultRate(
                                        categoryName)
                        });
                }

                CvSports.ItemsSource = null;
                CvSports.ItemsSource = _sports;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Loading Error",
                    $"Unable to load setup information:\n\n{ex.Message}",
                    "OK");
            }
        }

        // =========================================================
        // STEP 1 -> STEP 2
        // =========================================================

        private async void OnNextToStep2Clicked(
            object? sender,
            EventArgs e)
        {
            string arenaName =
                TxtArenaName.Text?.Trim() ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(arenaName))
            {
                await DisplayAlertAsync(
                    "Required",
                    "Please enter your arena/business name.",
                    "OK");

                return;
            }

            string email =
                TxtEmail.Text?.Trim() ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                await DisplayAlertAsync(
                    "Required",
                    "Please enter your business email.",
                    "OK");

                return;
            }

            if (!IsValidEmail(email))
            {
                await DisplayAlertAsync(
                    "Invalid Email",
                    "Please enter a valid business email address.",
                    "OK");

                return;
            }

            string phone =
                TxtPhone.Text?.Trim() ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(phone))
            {
                await DisplayAlertAsync(
                    "Required",
                    "Please enter your contact number.",
                    "OK");

                return;
            }

            Step1Layout.IsVisible = false;
            Step2Layout.IsVisible = true;
            Step3Layout.IsVisible = false;
        }

        // =========================================================
        // STEP 2 -> STEP 1
        // =========================================================

        private void OnBackToStep1Clicked(
            object? sender,
            EventArgs e)
        {
            Step1Layout.IsVisible = true;
            Step2Layout.IsVisible = false;
            Step3Layout.IsVisible = false;
        }

        // =========================================================
        // STEP 2 -> STEP 3
        // =========================================================

        private async void OnNextToStep3Clicked(
            object? sender,
            EventArgs e)
        {
            string location =
                TxtLocation.Text?.Trim() ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(location))
            {
                await DisplayAlertAsync(
                    "Required",
                    "Please specify your facility location/address.",
                    "OK");

                return;
            }

            // TimePicker.Time is NON-NULLABLE.
            TimeSpan openingTime = TpOpening.Time ?? new TimeSpan(8, 0, 0);
            TimeSpan closingTime = TpClosing.Time ?? new TimeSpan(23, 0, 0);

            // Equal opening/closing means zero business hours.
            //
            // Earlier closing time is allowed because the arena
            // may operate overnight, e.g.:
            //
            // 09:00 AM -> 02:00 AM

            if (openingTime == closingTime)
            {
                await DisplayAlertAsync(
                    "Invalid Timings",
                    "Opening and closing time cannot be the same.",
                    "OK");

                return;
            }

            Step1Layout.IsVisible = false;
            Step2Layout.IsVisible = false;
            Step3Layout.IsVisible = true;
        }

        // =========================================================
        // STEP 3 -> STEP 2
        // =========================================================

        private void OnBackToStep2Clicked(
            object? sender,
            EventArgs e)
        {
            Step1Layout.IsVisible = false;
            Step2Layout.IsVisible = true;
            Step3Layout.IsVisible = false;
        }

        // =========================================================
        // FINISH SETUP
        // =========================================================

        private async void OnFinishSetupClicked(
            object? sender,
            EventArgs e)
        {
            if (_isSaving)
                return;

            _isSaving = true;

            try
            {
                // =================================================
                // BASIC VALIDATION
                // =================================================

                if (_arenaId <= 0)
                {
                    await DisplayAlertAsync(
                        "Setup Error",
                        "Invalid arena account. Please register or log in again.",
                        "OK");

                    return;
                }

                string arenaName =
                    TxtArenaName.Text?.Trim() ??
                    string.Empty;

                if (string.IsNullOrWhiteSpace(arenaName))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Arena/business name is required.",
                        "OK");

                    return;
                }

                string email =
                    TxtEmail.Text?.Trim() ??
                    string.Empty;

                if (!IsValidEmail(email))
                {
                    await DisplayAlertAsync(
                        "Invalid Email",
                        "Please enter a valid business email address.",
                        "OK");

                    return;
                }

                string phone =
                    TxtPhone.Text?.Trim() ??
                    string.Empty;

                if (string.IsNullOrWhiteSpace(phone))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Contact number is required.",
                        "OK");

                    return;
                }

                string location =
                    TxtLocation.Text?.Trim() ??
                    string.Empty;

                if (string.IsNullOrWhiteSpace(location))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Facility location/address is required.",
                        "OK");

                    return;
                }

                // =================================================
                // TIME VALUES
                // =================================================
                TimeSpan openingTime =
                    TpOpening.Time ?? new TimeSpan(8, 0, 0);

                TimeSpan closingTime =
                    TpClosing.Time ?? new TimeSpan(23, 0, 0);

                // Allow overnight operation.
                //
                // Examples:
                // 08:00 -> 23:00  valid
                // 09:00 -> 02:00  valid
                // 08:00 -> 08:00  invalid

                if (openingTime == closingTime)
                {
                    await DisplayAlertAsync(
                        "Invalid Timings",
                        "Opening and closing time cannot be the same.",
                        "OK");

                    return;
                }

                // =================================================
                // SPORTS
                // =================================================

                if (_sports.Count == 0)
                {
                    await DisplayAlertAsync(
                        "Sports Error",
                        "No sports/categories were found in the database.",
                        "OK");

                    return;
                }

                var selectedSports =
                    _sports
                        .Where(s => s.IsSelected)
                        .ToList();

                if (selectedSports.Count == 0)
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please select at least one sport/court.",
                        "OK");

                    return;
                }

                // =================================================
                // VALIDATE QUANTITIES
                // =================================================

                foreach (var sport in selectedSports)
                {
                    if (!await ValidateSelectedQuantityAsync(sport))
                        return;
                }

                // =================================================
                // FIND ARENA
                // =================================================

                var existingArena =
                    await _dbContext.Arenas
                        .FirstOrDefaultAsync(
                            a => a.ArenaID == _arenaId);

                if (existingArena == null)
                {
                    await DisplayAlertAsync(
                        "Setup Error",
                        "The selected arena could not be found.",
                        "OK");

                    return;
                }

                // =================================================
                // EMAIL CONFLICT
                // =================================================

                bool emailUsedByAnotherArena =
                    await _dbContext.Arenas
                        .AnyAsync(
                            a =>
                                a.Email == email &&
                                a.ArenaID !=
                                existingArena.ArenaID);

                if (emailUsedByAnotherArena)
                {
                    await DisplayAlertAsync(
                        "Email Already Exists",
                        "This email is already registered with another arena.",
                        "OK");

                    return;
                }

                // =================================================
                // DATABASE TRANSACTION
                // =================================================

                await using var transaction =
                    await _dbContext.Database
                        .BeginTransactionAsync();

                // =================================================
                // UPDATE ARENA
                // =================================================

                existingArena.ArenaName =
                    arenaName;

                existingArena.Location =
                    location;

                existingArena.Email =
                    email;

                existingArena.PhoneNumber =
                    phone;

                existingArena.OpeningTime =
                    openingTime;

                existingArena.ClosingTime =
                    closingTime;

                // =================================================
                // DEFAULT COVER IMAGE
                // =================================================

                if (string.IsNullOrWhiteSpace(
                    existingArena.CoverImageUrl))
                {
                    existingArena.CoverImageUrl =
                        "https://images.unsplash.com/photo-1540747913346-19e32dc3e97e?auto=format&fit=crop&w=600&q=80";
                }

                // =================================================
                // SYNC ALL SPORTS
                // =================================================

                foreach (var sport in _sports)
                {
                    await SyncSportSelectionAsync(
                        sport,
                        existingArena.ArenaID);
                }

                // =================================================
                // MARK SETUP COMPLETE
                // =================================================

                existingArena.IsSetupCompleted =
                    true;

                // =================================================
                // SAVE
                // =================================================

                await _dbContext.SaveChangesAsync();

                // =================================================
                // COMMIT
                // =================================================

                await transaction.CommitAsync();

                // =================================================
                // SUCCESS
                // =================================================

                await DisplayAlertAsync(
                    "Setup Complete!",
                    "Your business profile and sports facilities have been successfully configured.",
                    "OK");

                // =================================================
                // DASHBOARD
                // =================================================

                await Navigation.PushAsync(
                    new DashboardPage(
                        _dbContext,
                        existingArena.ArenaID));
            }
            catch (DbUpdateException ex)
            {
                await DisplayAlertAsync(
                    "Database Error",
                    ex.InnerException?.Message ??
                    ex.Message,
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"Something went wrong while completing setup:\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                _isSaving = false;
            }
        }

        // =========================================================
        // VALIDATE SPORT QUANTITY
        // =========================================================

        private async Task<bool> ValidateSelectedQuantityAsync(
            SportSetupItem sport)
        {
            if (!sport.IsSelected)
                return true;

            string quantity =
                sport.Quantity?.Trim() ??
                string.Empty;

            if (!int.TryParse(
                    quantity,
                    out int count))
            {
                await DisplayAlertAsync(
                    "Invalid Quantity",
                    $"Please enter a valid quantity for {sport.CategoryName}.",
                    "OK");

                return false;
            }

            if (count <= 0)
            {
                await DisplayAlertAsync(
                    "Invalid Quantity",
                    $"Quantity for {sport.CategoryName} must be greater than zero.",
                    "OK");

                return false;
            }

            if (count > 100)
            {
                await DisplayAlertAsync(
                    "Invalid Quantity",
                    $"You cannot create more than 100 units for {sport.CategoryName}.",
                    "OK");

                return false;
            }

            return true;
        }

        // =========================================================
        // SYNC SPORT / COURTS
        // =========================================================

        private async Task SyncSportSelectionAsync(
            SportSetupItem sport,
            int arenaId)
        {
            var existingItems =
                await _dbContext.CourtsAndMenuItems
                    .Where(c =>
                        c.ArenaID == arenaId &&
                        c.CategoryID ==
                        sport.CategoryID)
                    .OrderBy(c => c.ItemID)
                    .ToListAsync();

            // =====================================================
            // SPORT UNSELECTED
            // =====================================================

            if (!sport.IsSelected)
            {
                // Do not physically delete courts because bookings
                // may reference them.
                //
                // Instead make them unavailable.

                foreach (var item in existingItems)
                {
                    item.IsAvailable = false;
                }

                return;
            }

            // =====================================================
            // QUANTITY
            // =====================================================

            if (!int.TryParse(
                    sport.Quantity?.Trim(),
                    out int requestedCount))
            {
                throw new InvalidOperationException(
                    $"Invalid quantity for {sport.CategoryName}.");
            }

            if (requestedCount <= 0)
            {
                throw new InvalidOperationException(
                    $"Quantity for {sport.CategoryName} must be greater than zero.");
            }

            // =====================================================
            // CATEGORY
            // =====================================================

            var category =
                await _dbContext.Categories
                    .FirstOrDefaultAsync(
                        c =>
                            c.CategoryID ==
                            sport.CategoryID);

            if (category == null)
            {
                throw new InvalidOperationException(
                    $"Category '{sport.CategoryName}' was not found.");
            }

            // =====================================================
            // RATE
            // =====================================================

            decimal rate =
                sport.DefaultRate;

            if (rate <= 0)
            {
                rate =
                    GetDefaultRate(
                        category.CategoryName);
            }

            // =====================================================
            // REACTIVATE / CREATE
            // =====================================================

            int currentTotal =
                existingItems.Count;

            // First reactivate existing inactive units.
            foreach (var item in existingItems)
            {
                if (currentTotal >= requestedCount)
                    break;

                item.IsAvailable = true;
            }

            // Number of currently available units.
            int availableCount =
                existingItems.Count(
                    c => c.IsAvailable);

            // =====================================================
            // CREATE NEW UNITS IF NEEDED
            // =====================================================

            if (availableCount < requestedCount)
            {
                int unitsToCreate =
                    requestedCount -
                    availableCount;

                int nextUnitNumber =
                    existingItems.Count + 1;

                for (int i = 0;
                     i < unitsToCreate;
                     i++)
                {
                    var newItem =
                        new CourtOrMenuItem
                        {
                            ArenaID =
                                arenaId,

                            CategoryID =
                                category.CategoryID,

                            ItemName =
                                $"{category.CategoryName} - Unit {nextUnitNumber}",

                            PricePerHour =
                                rate,

                            IsAvailable =
                                true,

                            ImageUrl =
                                GetSportImageUrl(
                                    category.CategoryName)
                        };

                    _dbContext.CourtsAndMenuItems.Add(
                        newItem);

                    nextUnitNumber++;
                }
            }

            // =====================================================
            // REDUCE QUANTITY
            // =====================================================

            if (availableCount > requestedCount)
            {
                var unitsToDisable =
                    existingItems
                        .Where(c => c.IsAvailable)
                        .OrderByDescending(c => c.ItemID)
                        .Take(
                            availableCount -
                            requestedCount)
                        .ToList();

                foreach (var item in unitsToDisable)
                {
                    item.IsAvailable = false;
                }
            }

            // =====================================================
            // UPDATE IMAGE URL FOR EXISTING ITEMS
            // =====================================================

            string imageUrl =
                GetSportImageUrl(
                    category.CategoryName);

            foreach (var item in existingItems)
            {
                if (string.IsNullOrWhiteSpace(
                    item.ImageUrl))
                {
                    item.ImageUrl = imageUrl;
                }
            }
        }

        // =========================================================
        // SPORT DESCRIPTIONS
        // =========================================================

        private static string GetSportDescription(
            string categoryName)
        {
            return categoryName
                .Trim()
                .ToLowerInvariant()
                .Replace("-", " ")
                .Replace("_", " ") switch
            {
                "indoor futsal" =>
                    "Indoor football court",

                "indoor cricket" =>
                    "Indoor cricket facility",

                "badminton" =>
                    "Badminton court",

                "table tennis" =>
                    "Table tennis tables",

                "basketball" =>
                    "Basketball court",

                "squash" =>
                    "Squash court",

                "indoor volleyball" =>
                    "Indoor volleyball court",

                "billiards & pool" =>
                    "Billiards and pool tables",

                "indoor boxing" =>
                    "Boxing training facility",

                "fitness & gym" =>
                    "Fitness and gym facility",

                "snooker room" =>
                    "Snooker tables",

                "yoga & pilates" =>
                    "Yoga and Pilates studio",

                "indoor board games" =>
                    "Indoor board games area",

                "indoor climbing" =>
                    "Indoor climbing facility",

                "cricket" =>
                    "Cricket facility",

                "martial arts dojo" =>
                    "Martial arts training area",

                _ =>
                    "Sports facility"
            };
        }

        // =========================================================
        // SPORT IMAGES
        // =========================================================

        private static string GetSportImageUrl(
            string categoryName)
        {
            return categoryName
                .Trim()
                .ToLowerInvariant()
                .Replace("-", " ")
                .Replace("_", " ") switch
            {
                "indoor futsal" or "futsal" =>
                    "https://images.unsplash.com/photo-1553778263-73a83bab9b0c?auto=format&fit=crop&w=800&q=80",

                "indoor cricket" or "cricket" =>
                    "https://images.unsplash.com/photo-1531415074968-036ba1b575da?auto=format&fit=crop&w=800&q=80",

                "badminton" =>
                    "https://images.unsplash.com/photo-1626224583764-f87db24ac4ea?auto=format&fit=crop&w=800&q=80",

                "table tennis" =>
                    "https://images.unsplash.com/photo-1558027400-0c9a7a3c1c9d?auto=format&fit=crop&w=800&q=80",

                "basketball" =>
                    "https://images.unsplash.com/photo-1546519638-68e109498ffc?auto=format&fit=crop&w=800&q=80",

                "squash" =>
                    "https://images.unsplash.com/photo-1595435934249-5df7ed86e1c0?auto=format&fit=crop&w=800&q=80",

                "indoor volleyball" =>
                    "https://images.unsplash.com/photo-1612872087720-bb876e2e67d1?auto=format&fit=crop&w=800&q=80",

                "billiards & pool" or "snooker room" =>
                    "https://images.unsplash.com/photo-1604167507553-9c2d0d4f6a45?auto=format&fit=crop&w=800&q=80",

                "indoor boxing" =>
                    "https://images.unsplash.com/photo-1549719386-74dfcbf7dbed?auto=format&fit=crop&w=800&q=80",

                "fitness & gym" =>
                    "https://images.unsplash.com/photo-1534438327276-14e5300c3a48?auto=format&fit=crop&w=800&q=80",

                "yoga & pilates" =>
                    "https://images.unsplash.com/photo-1544367567-0f2fcb009e0b?auto=format&fit=crop&w=800&q=80",

                "indoor board games" =>
                    "https://images.unsplash.com/photo-1610890716171-6b1bb98ffd09?auto=format&fit=crop&w=800&q=80",

                "indoor climbing" =>
                    "https://images.unsplash.com/photo-1522163182402-834f871fd851?auto=format&fit=crop&w=800&q=80",

                "martial arts dojo" =>
                    "https://images.unsplash.com/photo-1555597673-b21d5c935865?auto=format&fit=crop&w=800&q=80",

                _ =>
                    "https://images.unsplash.com/photo-1461896836934-ffe607ba8211?auto=format&fit=crop&w=800&q=80"
            };
        }

        // =========================================================
        // DEFAULT RATES
        // =========================================================

        private static decimal GetDefaultRate(
            string categoryName)
        {
            return categoryName
                .Trim()
                .ToLowerInvariant()
                .Replace("-", " ")
                .Replace("_", " ") switch
            {
                "indoor futsal" => 4000m,
                "futsal" => 4000m,

                "indoor cricket" => 3500m,
                "cricket" => 3500m,

                "badminton" => 1500m,

                "table tennis" => 1000m,

                "basketball" => 3000m,

                "squash" => 2000m,

                "indoor volleyball" => 3000m,

                "billiards & pool" => 1500m,

                "indoor boxing" => 2500m,

                "fitness & gym" => 2500m,

                "snooker room" => 1500m,

                "yoga & pilates" => 1500m,

                "indoor board games" => 500m,

                "indoor climbing" => 2500m,

                "martial arts dojo" => 2000m,

                "padel tennis" => 5000m,

                "padel" => 5000m,

                _ => 2000m
            };
        }

        // =========================================================
        // EMAIL VALIDATION
        // =========================================================

        private static bool IsValidEmail(
            string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var address =
                    new MailAddress(email);

                return string.Equals(
                    address.Address,
                    email,
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        // =========================================================
        // SPORT SETUP MODEL
        // =========================================================

        private sealed class SportSetupItem
        {
            public int CategoryID { get; set; }

            public string CategoryName { get; set; }
                = string.Empty;

            public string Description { get; set; }
                = string.Empty;

            public bool IsSelected { get; set; }

            public string Quantity { get; set; }
                = string.Empty;

            public decimal DefaultRate { get; set; }
        }
    }
}
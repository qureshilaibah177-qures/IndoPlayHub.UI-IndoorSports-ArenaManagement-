using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using Microsoft.EntityFrameworkCore;

namespace IndoPlayHub.UI
{
    public partial class EditSportPage : ContentPage
    {
        private readonly AppDbContext _dbContext;
        private readonly int _arenaId;
        private readonly int _categoryId;

        private Arena? _arena;
        private Category? _category;

        private List<CourtOrMenuItem> _units =
            new List<CourtOrMenuItem>();

        private bool _isLoading;
        private bool _isSaving;

        public EditSportPage(
            AppDbContext dbContext,
            int arenaId,
            int categoryId)
        {
            InitializeComponent();

            _dbContext = dbContext;
            _arenaId = arenaId;
            _categoryId = categoryId;
        }

        // =========================================================
        // PAGE APPEARING
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await LoadSportAsync();
        }

        // =========================================================
        // LOAD SPORT
        // =========================================================

        private async Task LoadSportAsync()
        {
            if (_isLoading)
                return;

            _isLoading = true;

            try
            {
                if (_arenaId <= 0 || _categoryId <= 0)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "Invalid arena or sport.",
                        "OK");

                    return;
                }

                // =================================================
                // ARENA
                // =================================================

                _arena =
     await _dbContext.Arenas
         .FirstOrDefaultAsync(a =>
             a.ArenaID == _arenaId);

                if (_arena == null)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "Arena could not be found.",
                        "OK");

                    return;
                }

                // =================================================
                // CATEGORY
                // =================================================

                _category =
                    await _dbContext.Categories
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c =>
                            c.CategoryID == _categoryId);

                if (_category == null)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "Sport could not be found.",
                        "OK");

                    return;
                }

                // =================================================
                // HEADER
                // =================================================

                LblSportName.Text =
                    _category.CategoryName;

                LblCategory.Text =
                    _category.CategoryName;

                TpOpeningTime.Time =
     _arena.OpeningTime;

                TpClosingTime.Time =
                    _arena.ClosingTime;

                // =================================================
                // LOAD UNITS
                // =================================================

                _units =
                    await _dbContext.CourtsAndMenuItems
                        .Include(x => x.Bookings)
                        .Where(x =>
                            x.ArenaID == _arenaId &&
                            x.CategoryID == _categoryId)
                        .OrderBy(x => x.ItemID)
                        .ToListAsync();

                decimal averagePrice =
                    _units.Count > 0
                        ? _units.Average(x =>
                            x.PricePerHour)
                        : 0;

                TxtPrice.Text =
                    averagePrice.ToString("0.##");

                LblUnitCount.Text =
                    _units.Count.ToString();

                CvUnits.ItemsSource =
                    _units
                        .Select(x =>
                            new UnitDisplayItem
                            {
                                ItemID =
                                    x.ItemID,

                                ItemName =
                                    x.ItemName,

                                PriceText =
                                    $"Rs. {x.PricePerHour:N0} / hour",

                                BookingStatusText =
                                    GetBookingStatusText(x)
                            })
                        .ToList();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Load Error",
                    $"Could not load sport:\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                _isLoading = false;
            }
        }

        // =========================================================
        // BOOKING STATUS
        // =========================================================

        private string GetBookingStatusText(
            CourtOrMenuItem unit)
        {
            int bookings =
                unit.Bookings?.Count ?? 0;

            if (bookings == 0)
            {
                return unit.IsAvailable
                    ? "● Available • No bookings"
                    : "● Currently unavailable";
            }

            return unit.IsAvailable
                ? $"● Available • {bookings} booking(s)"
                : $"● Unavailable • {bookings} booking(s)";
        }

        // =========================================================
        // EDIT UNIT
        // =========================================================

        private async void OnEditUnitClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                if (sender is not Button button)
                    return;

                if (button.BindingContext is not UnitDisplayItem unit)
                    return;

                var selectedUnit =
                    _units.FirstOrDefault(x =>
                        x.ItemID == unit.ItemID);

                if (selectedUnit == null)
                    return;

                string? newName =
                    await DisplayPromptAsync(
                        "Edit Playing Unit",
                        "Unit name:",
                        initialValue:
                            selectedUnit.ItemName,
                        maxLength: 100,
                        keyboard:
                            Keyboard.Default);

                if (newName == null)
                    return;

                newName = newName.Trim();

                if (string.IsNullOrWhiteSpace(newName))
                {
                    await DisplayAlertAsync(
                        "Invalid Name",
                        "Unit name cannot be empty.",
                        "OK");

                    return;
                }

                string? newPriceText =
                    await DisplayPromptAsync(
                        "Hourly Rate",
                        "Price per hour:",
                        initialValue:
                            selectedUnit.PricePerHour.ToString("0.##"),
                        maxLength: 20,
                        keyboard:
                            Keyboard.Numeric);

                if (newPriceText == null)
                    return;

                if (!decimal.TryParse(
                        newPriceText,
                        out decimal newPrice) ||
                    newPrice < 0)
                {
                    await DisplayAlertAsync(
                        "Invalid Price",
                        "Please enter a valid hourly price.",
                        "OK");

                    return;
                }

                selectedUnit.ItemName =
                    newName;

                selectedUnit.PricePerHour =
                    newPrice;

                await _dbContext.SaveChangesAsync();

                await LoadSportAsync();

                await DisplayAlertAsync(
                    "Updated",
                    $"{newName} has been updated successfully.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Update Error",
                    ex.Message,
                    "OK");
            }
        }

        // =========================================================
        // ADD UNIT
        // =========================================================

        private async void OnAddUnitClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                if (_category == null)
                    return;

                string? unitName =
                    await DisplayPromptAsync(
                        "Add Playing Unit",
                        $"Enter the name for the new {_category.CategoryName} unit:",
                        placeholder:
                            $"{_category.CategoryName} Court",
                        maxLength: 100);

                if (unitName == null)
                    return;

                unitName = unitName.Trim();

                if (string.IsNullOrWhiteSpace(unitName))
                {
                    await DisplayAlertAsync(
                        "Invalid Name",
                        "Please enter a unit name.",
                        "OK");

                    return;
                }

                string? priceText =
                    await DisplayPromptAsync(
                        "Hourly Rate",
                        "Enter the price per hour:",
                        initialValue:
                            GetCurrentPrice().ToString("0.##"),
                        keyboard:
                            Keyboard.Numeric);

                if (priceText == null)
                    return;

                if (!decimal.TryParse(
                        priceText,
                        out decimal price) ||
                    price < 0)
                {
                    await DisplayAlertAsync(
                        "Invalid Price",
                        "Please enter a valid price.",
                        "OK");

                    return;
                }

                // =================================================
                // DUPLICATE NAME CHECK
                // =================================================

                bool duplicate =
                    await _dbContext.CourtsAndMenuItems
                        .AnyAsync(x =>
                            x.ArenaID == _arenaId &&
                            x.CategoryID == _categoryId &&
                            x.ItemName == unitName);

                if (duplicate)
                {
                    await DisplayAlertAsync(
                        "Already Exists",
                        "A playing unit with this name already exists.",
                        "OK");

                    return;
                }

                var newUnit =
                    new CourtOrMenuItem
                    {
                        ArenaID =
                            _arenaId,

                        CategoryID =
                            _categoryId,

                        ItemName =
                            unitName,

                        PricePerHour =
                            price,

                        IsAvailable =
                            true
                    };

                _dbContext.CourtsAndMenuItems.Add(
                    newUnit);

                await _dbContext.SaveChangesAsync();

                await LoadSportAsync();

                await DisplayAlertAsync(
                    "Unit Added",
                    $"{unitName} has been added successfully.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Add Unit Error",
                    ex.Message,
                    "OK");
            }
        }

        // =========================================================
        // CURRENT PRICE
        // =========================================================

        private decimal GetCurrentPrice()
        {
            if (decimal.TryParse(
                    TxtPrice.Text,
                    out decimal price))
            {
                return price;
            }

            if (_units.Count > 0)
            {
                return _units[0].PricePerHour;
            }

            return 0;
        }

        // =========================================================
        // SAVE SPORT
        // =========================================================

        private async void OnSaveClicked(
            object? sender,
            EventArgs e)
        {
            if (_isSaving)
                return;

            _isSaving = true;

            try
            {
                if (_units.Count == 0)
                {
                    await DisplayAlertAsync(
                        "No Units",
                        "Please add at least one playing unit.",
                        "OK");

                    return;
                }

                if (!decimal.TryParse(
                        TxtPrice.Text,
                        out decimal price) ||
                    price < 0)
                {
                    await DisplayAlertAsync(
                        "Invalid Price",
                        "Please enter a valid price per hour.",
                        "OK");

                    return;
                }

                // =================================================
                // APPLY PRICE TO ALL UNITS
                // =================================================

                foreach (var unit in _units)
                {
                    unit.PricePerHour =
                        price;
                }
                if (_arena == null)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "Arena information could not be loaded.",
                        "OK");

                    return;
                }
                TimeSpan openingTime =
                    TpOpeningTime.Time ?? TimeSpan.Zero;

                TimeSpan closingTime =
                    TpClosingTime.Time ?? TimeSpan.Zero;
                if (openingTime >= closingTime)
                {
                    await DisplayAlertAsync(
                        "Invalid Timings",
                        "Opening time must be earlier than closing time.",
                        "OK");

                    return;
                }

                _arena.OpeningTime =
                    openingTime;

                _arena.ClosingTime =
                    closingTime;
                await _dbContext.SaveChangesAsync();

                await DisplayAlertAsync(
                    "Saved",
                    $"{_category?.CategoryName} changes have been saved successfully.",
                    "OK");

                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Save Error",
                    $"Could not save changes:\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                _isSaving = false;
            }
        }

        // =========================================================
        // BACK
        // =========================================================

        private async void OnBackClicked(
            object? sender,
            EventArgs e)
        {
            await Navigation.PopAsync();
        }

        // =========================================================
        // DISPLAY MODEL
        // =========================================================

        private class UnitDisplayItem
        {
            public int ItemID { get; set; }

            public string ItemName { get; set; } =
                string.Empty;

            public string PriceText { get; set; } =
                string.Empty;

            public string BookingStatusText { get; set; } =
                string.Empty;
        }
    }
}
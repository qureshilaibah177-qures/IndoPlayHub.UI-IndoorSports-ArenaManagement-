using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using Microsoft.EntityFrameworkCore;

namespace IndoPlayHub.UI
{
    public partial class AddSportPage : ContentPage
    {
        private readonly AppDbContext _dbContext;
        private readonly int _arenaId;

        private List<Category> _categories =
            new List<Category>();

        private bool _isLoading;
        private bool _isSaving;

        public AddSportPage(
            AppDbContext dbContext,
            int arenaId)
        {
            InitializeComponent();

            _dbContext = dbContext;
            _arenaId = arenaId;
        }

        // =========================================================
        // PAGE APPEARING
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await LoadCategoriesAsync();
        }

        // =========================================================
        // LOAD AVAILABLE SPORTS
        // =========================================================

        private async Task LoadCategoriesAsync()
        {
            if (_isLoading)
                return;

            _isLoading = true;

            try
            {
                if (_arenaId <= 0)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "Invalid arena ID.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // VERIFY ARENA
                // -------------------------------------------------

                bool arenaExists =
                    await _dbContext.Arenas
                        .AsNoTracking()
                        .AnyAsync(a =>
                            a.ArenaID == _arenaId);

                if (!arenaExists)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "Arena could not be found.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // LOAD CATEGORIES
                // -------------------------------------------------

                _categories =
                    await _dbContext.Categories
                        .AsNoTracking()
                        .OrderBy(c => c.CategoryName)
                        .ToListAsync();

                PkSport.ItemsSource =
                    _categories;

                if (_categories.Count == 0)
                {
                    await DisplayAlertAsync(
                        "No Sports",
                        "No sports/categories are available in the database.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Load Error",
                    $"Could not load sports:\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                _isLoading = false;
            }
        }

        // =========================================================
        // SPORT SELECTED
        // =========================================================

        private async void OnSportSelected(
            object? sender,
            EventArgs e)
        {
            try
            {
                if (PkSport.SelectedItem is not Category category)
                    return;

                bool alreadyConfigured =
                    await _dbContext.CourtsAndMenuItems
                        .AsNoTracking()
                        .AnyAsync(x =>
                            x.ArenaID == _arenaId &&
                            x.CategoryID == category.CategoryID);

                if (alreadyConfigured)
                {
                    LblPreview.Text =
                        $"{category.CategoryName} is already configured " +
                        "for this arena.\n\nUse Edit Sport to add more " +
                        "playing units or change its price.";

                    LblPreview.TextColor =
                        Color.FromArgb("#FFB74D");

                    BtnAddSport.IsEnabled = false;

                    return;
                }

                LblPreview.Text =
                    $"This will add {category.CategoryName} " +
                    "to your arena.";

                LblPreview.TextColor =
                    Color.FromArgb("#D0D5DD");

                BtnAddSport.IsEnabled = true;

                // Sensible default
                if (string.IsNullOrWhiteSpace(TxtUnitCount.Text))
                    TxtUnitCount.Text = "1";
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Sport Error",
                    ex.Message,
                    "OK");
            }
        }

        // =========================================================
        // ADD SPORT
        // =========================================================

        private async void OnAddSportClicked(
            object? sender,
            EventArgs e)
        {
            if (_isSaving)
                return;

            _isSaving = true;

            BtnAddSport.IsEnabled = false;

            try
            {
                // =================================================
                // VALIDATE SPORT
                // =================================================

                if (PkSport.SelectedItem is not Category category)
                {
                    await DisplayAlertAsync(
                        "Select Sport",
                        "Please select a sport first.",
                        "OK");

                    return;
                }

                // =================================================
                // VALIDATE UNIT COUNT
                // =================================================

                if (!int.TryParse(
                        TxtUnitCount.Text,
                        out int unitCount) ||
                    unitCount <= 0)
                {
                    await DisplayAlertAsync(
                        "Invalid Units",
                        "Please enter a valid number of playing units.",
                        "OK");

                    return;
                }

                // Safety limit
                if (unitCount > 100)
                {
                    await DisplayAlertAsync(
                        "Too Many Units",
                        "You cannot add more than 100 playing units at once.",
                        "OK");

                    return;
                }

                // =================================================
                // VALIDATE PRICE
                // =================================================

                if (!decimal.TryParse(
                        TxtPrice.Text,
                        out decimal price) ||
                    price < 0)
                {
                    await DisplayAlertAsync(
                        "Invalid Price",
                        "Please enter a valid hourly price.",
                        "OK");

                    return;
                }

                // =================================================
                // DUPLICATE SPORT CHECK
                // =================================================

                bool alreadyExists =
                    await _dbContext.CourtsAndMenuItems
                        .AsNoTracking()
                        .AnyAsync(x =>
                            x.ArenaID == _arenaId &&
                            x.CategoryID == category.CategoryID);

                if (alreadyExists)
                {
                    await DisplayAlertAsync(
                        "Sport Already Exists",
                        $"{category.CategoryName} is already configured " +
                        "for this arena.\n\nUse Edit Sport to manage it.",
                        "OK");

                    return;
                }

                // =================================================
                // CREATE UNITS
                // =================================================

                var newUnits =
                    new List<CourtOrMenuItem>();

                for (int i = 1; i <= unitCount; i++)
                {
                    newUnits.Add(
                        new CourtOrMenuItem
                        {
                            ArenaID =
                                _arenaId,

                            CategoryID =
                                category.CategoryID,

                            ItemName =
                                $"{category.CategoryName} {i}",

                            PricePerHour =
                                price,

                            IsAvailable =
                                true
                        });
                }

                // =================================================
                // SAVE
                // =================================================

                await using var transaction =
                    await _dbContext.Database
                        .BeginTransactionAsync();

                try
                {
                    await _dbContext.CourtsAndMenuItems
                        .AddRangeAsync(newUnits);

                    await _dbContext.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                // =================================================
                // SUCCESS
                // =================================================

                await DisplayAlertAsync(
                    "Sport Added",
                    $"{category.CategoryName} has been added successfully " +
                    $"with {unitCount} playing unit(s).",
                    "OK");

                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Add Sport Error",
                    $"Could not add the sport:\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                _isSaving = false;

                if (PkSport.SelectedItem == null)
                    BtnAddSport.IsEnabled = true;
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
    }
}
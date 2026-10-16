using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using Microsoft.EntityFrameworkCore;

namespace IndoPlayHub.UI
{
    public partial class CourtsPage : ContentPage
    {
        private readonly AppDbContext _dbContext;
        private readonly int _currentArenaId;

        public CourtsPage(
            AppDbContext dbContext,
            int currentArenaId)
        {
            InitializeComponent();

            _dbContext = dbContext;
            _currentArenaId = currentArenaId;

            _ = InitializePageDataAsync();
        }


        // =========================================================
        // INITIALIZE PAGE
        // =========================================================

        private async Task InitializePageDataAsync()
        {
            await LoadCategoriesAsync();
            await LoadCourtsAsync();
        }


        // =========================================================
        // LOAD SPORTS
        // =========================================================

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var categories =
                    await _dbContext.Categories
                        .AsNoTracking()
                        .OrderBy(c => c.CategoryName)
                        .ToListAsync();

                PickerCategory.ItemsSource = categories;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"Failed to load sport categories:\n\n{ex.Message}",
                    "OK");
            }
        }


        // =========================================================
        // LOAD COURTS
        // =========================================================

        private async Task LoadCourtsAsync()
        {
            try
            {
                var courts =
                    await _dbContext.CourtsAndMenuItems
                        .AsNoTracking()
                        .Where(c =>
                            c.ArenaID == _currentArenaId)
                        .OrderBy(c => c.ItemName)
                        .ToListAsync();

                CollectionViewCourts.ItemsSource = courts;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"Failed to load courts:\n\n{ex.Message}",
                    "OK");
            }
        }


        // =========================================================
        // SAVE COURT
        // =========================================================

        private async void OnSaveCourtClicked(
            object sender,
            EventArgs e)
        {
            string courtName =
                TxtCourtName.Text?.Trim()
                ?? string.Empty;

            var selectedCategory =
                PickerCategory.SelectedItem as Category;

            string priceText =
                TxtPrice.Text?.Trim()
                ?? string.Empty;


            // -----------------------------------------------------
            // VALIDATION
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(courtName) ||
                selectedCategory == null ||
                string.IsNullOrWhiteSpace(priceText))
            {
                await DisplayAlertAsync(
                    "Validation Error",
                    "Please fill in all required fields and select a sport category.",
                    "OK");

                return;
            }


            // -----------------------------------------------------
            // PRICE
            // -----------------------------------------------------

            if (!decimal.TryParse(
                    priceText,
                    out decimal basePrice))
            {
                await DisplayAlertAsync(
                    "Validation Error",
                    "Please enter a valid numeric price.",
                    "OK");

                return;
            }

            if (basePrice <= 0)
            {
                await DisplayAlertAsync(
                    "Validation Error",
                    "Price must be greater than zero.",
                    "OK");

                return;
            }


            // -----------------------------------------------------
            // ADD-ONS
            // -----------------------------------------------------

            decimal extraFee = 0;

            if (ChkFloodlights.IsChecked)
                extraFee += 500;

            if (ChkEquipment.IsChecked)
                extraFee += 300;


            decimal finalPricePerHour =
                basePrice + extraFee;


            // -----------------------------------------------------
            // SAVE
            // -----------------------------------------------------

            try
            {
                var newCourt =
                    new CourtOrMenuItem
                    {
                        ArenaID =
                            _currentArenaId,

                        CategoryID =
                            selectedCategory.CategoryID,

                        ItemName =
                            courtName,

                        PricePerHour =
                            finalPricePerHour,

                        IsAvailable =
                            true
                    };

                _dbContext.CourtsAndMenuItems
                    .Add(newCourt);

                await _dbContext.SaveChangesAsync();


                // -------------------------------------------------
                // SUCCESS MESSAGE
                // -------------------------------------------------

                string message =
                    "Court registered successfully!";

                if (extraFee > 0)
                {
                    message +=
                        $"\n\nBase: Rs. {basePrice:N0}" +
                        $"\nAdd-ons: Rs. {extraFee:N0}" +
                        $"\nTotal: Rs. {finalPricePerHour:N0}/hr";
                }

                await DisplayAlertAsync(
                    "Success",
                    message,
                    "OK");


                // -------------------------------------------------
                // RESET
                // -------------------------------------------------

                TxtCourtName.Text =
                    string.Empty;

                TxtPrice.Text =
                    string.Empty;

                ChkFloodlights.IsChecked =
                    false;

                ChkEquipment.IsChecked =
                    false;

                PickerCategory.SelectedItem =
                    null;


                // -------------------------------------------------
                // REFRESH
                // -------------------------------------------------

                await LoadCourtsAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Database Error",
                    ex.Message,
                    "OK");
            }
        }


        // =========================================================
        // DELETE COURT
        // =========================================================

        private async void OnDeleteCourtClicked(
            object sender,
            EventArgs e)
        {
            if (sender is not Button button ||
                button.CommandParameter is not int itemId)
            {
                return;
            }


            bool confirm =
                await DisplayAlertAsync(
                    "Confirm Delete",
                    "Are you sure you want to remove this court?",
                    "Yes",
                    "No");

            if (!confirm)
                return;


            try
            {
                var court =
                    await _dbContext
                        .CourtsAndMenuItems
                        .FindAsync(itemId);

                if (court == null)
                {
                    await DisplayAlertAsync(
                        "Not Found",
                        "The selected court could not be found.",
                        "OK");

                    return;
                }


                _dbContext.CourtsAndMenuItems
                    .Remove(court);

                await _dbContext.SaveChangesAsync();

                await LoadCourtsAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Delete Error",
                    ex.Message,
                    "OK");
            }
        }
    }
}
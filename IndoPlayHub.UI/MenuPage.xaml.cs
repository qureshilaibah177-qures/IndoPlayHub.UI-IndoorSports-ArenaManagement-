using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace IndoPlayHub.UI
{
    public partial class MenuPage : ContentPage
    {
        private readonly AppDbContext _dbContext;
        private readonly int _currentArenaId;

        private bool _isLoading;

        public MenuPage(
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

            await LoadMenuItemsAsync();
        }

        // =========================================================
        // LOAD MENU ITEMS
        // =========================================================

        private async Task LoadMenuItemsAsync()
        {
            if (_isLoading)
                return;

            _isLoading = true;

            try
            {
                // -------------------------------------------------
                // CHECK ARENA
                // -------------------------------------------------

                bool arenaExists = await _dbContext.Arenas
                    .AsNoTracking()
                    .AnyAsync(a => a.ArenaID == _currentArenaId);

                if (!arenaExists)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "The selected arena could not be found.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // LOAD MENU ITEMS
                // -------------------------------------------------

                var items = await _dbContext.MenuItems
                    .AsNoTracking()
                    .Include(x => x.MenuCategory)
                    .Where(x => x.ArenaID == _currentArenaId)
                    .OrderBy(x => x.ItemName)
                    .ToListAsync();

                // -------------------------------------------------
                // DISPLAY
                // -------------------------------------------------

                CvMenuItems.ItemsSource = items;

                LblItemCount.Text = items.Count == 1
                    ? "1 item"
                    : $"{items.Count} items";

                EmptyState.IsVisible = items.Count == 0;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"Failed to load menu items.\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                _isLoading = false;
            }
        }

        // =========================================================
        // SAVE MENU ITEM
        // =========================================================

        private async void OnSaveMenuItemClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                // -------------------------------------------------
                // READ FORM
                // -------------------------------------------------

                string itemName =
                    TxtItemName.Text?.Trim() ?? string.Empty;

                string priceText =
                    TxtPrice.Text?.Trim() ?? string.Empty;

                string imageUrl =
                    TxtItemImage.Text?.Trim() ?? string.Empty;

                // -------------------------------------------------
                // ITEM NAME VALIDATION
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(itemName))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter the item name.",
                        "OK");

                    return;
                }

                if (itemName.Length > 100)
                {
                    await DisplayAlertAsync(
                        "Invalid Name",
                        "Item name cannot exceed 100 characters.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // PRICE VALIDATION
                // -------------------------------------------------

                if (!decimal.TryParse(
                        priceText,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out decimal price))
                {
                    await DisplayAlertAsync(
                        "Invalid Price",
                        "Please enter a valid numeric price.",
                        "OK");

                    return;
                }

                if (price <= 0)
                {
                    await DisplayAlertAsync(
                        "Invalid Price",
                        "Price must be greater than zero.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // IMAGE URL
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    imageUrl = "arena_bg.jpg";
                }
                else
                {
                    if (imageUrl.Length > 255)
                    {
                        await DisplayAlertAsync(
                            "Invalid Image URL",
                            "Image URL cannot exceed 255 characters.",
                            "OK");

                        return;
                    }

                    if (!Uri.TryCreate(
                            imageUrl,
                            UriKind.Absolute,
                            out Uri? imageUri) ||
                        (imageUri.Scheme != Uri.UriSchemeHttp &&
                         imageUri.Scheme != Uri.UriSchemeHttps))
                    {
                        await DisplayAlertAsync(
                            "Invalid Image URL",
                            "Please enter a valid HTTP or HTTPS image URL.",
                            "OK");

                        return;
                    }
                }

                // -------------------------------------------------
                // FIND MENU CATEGORY
                // -------------------------------------------------

                string[] possibleCategories =
                {
                    "Food",
                    "Beverage",
                    "Equipment",
                    "Store",
                    "Cafeteria"
                };

                var category = await _dbContext.MenuCategories
                    .FirstOrDefaultAsync(c =>
                        possibleCategories.Contains(
                            c.MenuCategoryName));

                if (category == null)
                {
                    await DisplayAlertAsync(
                        "Category Missing",
                        "No menu categories were found in the database.\n\n" +
                        "Please make sure the MenuCategories table contains " +
                        "Food, Beverage, Equipment, Store or Cafeteria.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // DUPLICATE CHECK
                // -------------------------------------------------

                bool duplicateExists =
                    await _dbContext.MenuItems
                        .AnyAsync(x =>
                            x.ArenaID == _currentArenaId &&
                            x.MenuCategoryID == category.MenuCategoryID &&
                            x.ItemName == itemName);

                if (duplicateExists)
                {
                    await DisplayAlertAsync(
                        "Duplicate Item",
                        $"'{itemName}' already exists in this menu category.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // CREATE MENU ITEM
                // IMPORTANT:
                // Use ArenaMenuItem, NOT MenuItem.
                // This avoids the MAUI MenuItem name conflict.
                // -------------------------------------------------

                var newItem = new ArenaMenuItem
                {
                    ArenaID = _currentArenaId,

                    MenuCategoryID =
                        category.MenuCategoryID,

                    ItemName = itemName,

                    Price = price,

                    IsAvailable = true,

                    ImageUrl = imageUrl
                };

                _dbContext.MenuItems.Add(newItem);

                await _dbContext.SaveChangesAsync();

                // -------------------------------------------------
                // RESET FORM
                // -------------------------------------------------

                TxtItemName.Text = string.Empty;
                TxtPrice.Text = string.Empty;
                TxtItemImage.Text = string.Empty;

                // -------------------------------------------------
                // SUCCESS
                // -------------------------------------------------

                await DisplayAlertAsync(
                    "Success",
                    $"{itemName} has been added successfully.",
                    "OK");

                await LoadMenuItemsAsync();
            }
            catch (DbUpdateException ex)
            {
                await DisplayAlertAsync(
                    "Database Error",
                    ex.InnerException?.Message ?? ex.Message,
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
        // DELETE MENU ITEM
        // =========================================================

        private async void OnDeleteMenuItemClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                // -------------------------------------------------
                // GET BUTTON
                // -------------------------------------------------

                if (sender is not Button button)
                    return;

                // -------------------------------------------------
                // GET MENU ITEM ID
                // -------------------------------------------------

                if (button.CommandParameter is not int menuItemId)
                    return;

                // -------------------------------------------------
                // FIND MENU ITEM
                // -------------------------------------------------

                var item = await _dbContext.MenuItems
                    .FirstOrDefaultAsync(x =>
                        x.MenuItemID == menuItemId &&
                        x.ArenaID == _currentArenaId);

                if (item == null)
                {
                    await DisplayAlertAsync(
                        "Not Found",
                        "The selected menu item could not be found.",
                        "OK");

                    await LoadMenuItemsAsync();

                    return;
                }

                // -------------------------------------------------
                // CONFIRM DELETE
                // -------------------------------------------------

                bool confirm = await DisplayAlertAsync(
                    "Delete Item",
                    $"Are you sure you want to delete '{item.ItemName}'?",
                    "Delete",
                    "Cancel");

                if (!confirm)
                    return;

                // -------------------------------------------------
                // DELETE
                // -------------------------------------------------

                _dbContext.MenuItems.Remove(item);

                await _dbContext.SaveChangesAsync();

                // -------------------------------------------------
                // SUCCESS
                // -------------------------------------------------

                await DisplayAlertAsync(
                    "Deleted",
                    $"{item.ItemName} has been removed successfully.",
                    "OK");

                await LoadMenuItemsAsync();
            }
            catch (DbUpdateException ex)
            {
                await DisplayAlertAsync(
                    "Database Error",
                    "This menu item could not be deleted.\n\n" +
                    (ex.InnerException?.Message ?? ex.Message),
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
    }
}
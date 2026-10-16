using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace IndoPlayHub.UI
{
    public partial class CustomerMenuPage :
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


        // =========================================================
        // DATA
        // =========================================================

        private readonly ObservableCollection<CustomerMenuItemModel>
            _allItems = new();

        private readonly ObservableCollection<CustomerMenuItemModel>
            _visibleItems = new();

        private readonly ObservableCollection<CustomerMenuCategoryModel>
            _categories = new();


        // =========================================================
        // STATE
        // =========================================================

        private bool _queryReceived;

        private bool _isLoading;

        private string _selectedCategory = "All";

        private string _searchText = string.Empty;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public CustomerMenuPage(
            AppDbContext dbContext)
        {
            InitializeComponent();

            _dbContext = dbContext;

            CvMenuItems.ItemsSource =
                _visibleItems;

            CvCategories.ItemsSource =
                _categories;

            UpdateCartDisplay();
        }


        // =========================================================
        // QUERY PARAMETERS
        // =========================================================

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            try
            {
                if (_queryReceived)
                    return;

                _queryReceived = true;

                _arenaID =
                    GetIntQueryValue(
                        query,
                        "ArenaID");

                _ = LoadMenuAsync();
            }
            catch (Exception ex)
            {
                _ = ShowErrorAsync(
                    "Menu",
                    $"Unable to open the arena menu.\n\n{ex.Message}");
            }
        }


        // =========================================================
        // QUERY INTEGER
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
        // LOAD MENU
        // =========================================================

        private async Task LoadMenuAsync()
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
                            x =>
                                x.ArenaID == _arenaID);

                if (arena == null)
                {
                    await ShowErrorAsync(
                        "Arena",
                        "The selected arena could not be found.");

                    return;
                }


                // =================================================
                // DISPLAY ARENA
                // =================================================

                LblArenaName.Text =
                    string.IsNullOrWhiteSpace(
                        arena.ArenaName)
                        ? "Arena Menu"
                        : arena.ArenaName;


                // =================================================
                // LOAD MENU ITEMS
                // =================================================

                List<ArenaMenuItem> databaseItems =
                    await _dbContext.MenuItems
                        .AsNoTracking()
                        .Include(x => x.MenuCategory)
                        .Where(
                            x =>
                                x.ArenaID == _arenaID)
                        .OrderBy(
                            x =>
                                x.ItemName)
                        .ToListAsync();


                // =================================================
                // BUILD CUSTOMER ITEMS
                // =================================================

                _allItems.Clear();

                foreach (
                    ArenaMenuItem item
                    in databaseItems)
                {
                    string categoryName =
                        item.MenuCategory
                            ?.MenuCategoryName
                        ?? "Other";


                    bool hasImage =
                        HasValidImageUrl(
                            item.ImageUrl);


                    CustomerMenuItemModel model =
                        new CustomerMenuItemModel
                        {
                            MenuItemID =
                                item.MenuItemID,

                            ArenaID =
                                item.ArenaID,

                            ItemName =
                                item.ItemName,

                            Price =
                                item.Price,

                            IsAvailable =
                                item.IsAvailable,

                            ImageUrl =
                                hasImage
                                    ? item.ImageUrl
                                    : null,

                            CategoryName =
                                categoryName,

                            HasImage =
                                hasImage,

                            Quantity = 0
                        };


                    _allItems.Add(model);
                }


                // =================================================
                // BUILD CUSTOMER CATEGORY BAR
                // =================================================

                BuildCategories();


                // =================================================
                // APPLY SEARCH / DISPLAY
                // =================================================

                ApplyFilters();


                // =================================================
                // EMPTY MESSAGE
                // =================================================

                if (_allItems.Count == 0)
                {
                    LblEmptyMessage.Text =
                        "This arena has not registered any menu items yet.";
                }
                else if (_visibleItems.Count == 0)
                {
                    LblEmptyMessage.Text =
                        "No menu items match your search.";
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(
                    "Menu Error",
                    $"Unable to load the arena menu.\n\n{ex.Message}");
            }
            finally
            {
                HideLoadingState();

                _isLoading = false;
            }
        }


        // =========================================================
        // BUILD CATEGORY FILTER
        // =========================================================

        private void BuildCategories()
        {
            _categories.Clear();

            _categories.Add(
                new CustomerMenuCategoryModel
                {
                    CategoryName =
                        "All",

                    BackgroundColor =
                        "#12302D",

                    BorderColor =
                        "#03DAC6",

                    TextColor =
                        "#03DAC6",

                    IsSelected =
                        true
                });

            CvCategories.ItemsSource =
                null;

            CvCategories.ItemsSource =
                _categories;
        }


        // =========================================================
        // CATEGORY CLICKED
        // =========================================================

        private void OnCategoryTapped(
            object? sender,
            TappedEventArgs e)
        {
            if (e.Parameter
                is not CustomerMenuCategoryModel category)
            {
                return;
            }


            _selectedCategory =
                "All";


            foreach (
                CustomerMenuCategoryModel item
                in _categories)
            {
                item.IsSelected =
                    true;

                item.BackgroundColor =
                    "#12302D";

                item.BorderColor =
                    "#03DAC6";

                item.TextColor =
                    "#03DAC6";
            }


            ApplyFilters();
        }


        // =========================================================
        // SEARCH
        // =========================================================

        private void OnSearchTextChanged(
            object? sender,
            TextChangedEventArgs e)
        {
            _searchText =
                e.NewTextValue?.Trim()
                ?? string.Empty;

            ApplyFilters();
        }


        // =========================================================
        // APPLY FILTERS
        // =========================================================

        private void ApplyFilters()
        {
            IEnumerable<CustomerMenuItemModel> query =
                _allItems;


            // =====================================================
            // NO CATEGORY FILTER
            // =====================================================

            // All menu items remain visible.


            // =====================================================
            // SEARCH
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                _searchText))
            {
                query =
                    query.Where(
                        x =>
                            x.ItemName.Contains(
                                _searchText,
                                StringComparison.OrdinalIgnoreCase)
                            ||
                            x.CategoryName.Contains(
                                _searchText,
                                StringComparison.OrdinalIgnoreCase));
            }


            // =====================================================
            // DISPLAY ORDER
            // =====================================================

            List<CustomerMenuItemModel> results =
                query
                    .OrderBy(
                        x =>
                            x.ItemName)
                    .ToList();


            _visibleItems.Clear();

            foreach (
                CustomerMenuItemModel item
                in results)
            {
                _visibleItems.Add(item);
            }


            // =====================================================
            // EMPTY STATE
            // =====================================================

            bool showEmpty =
                _visibleItems.Count == 0;


            CvMenuItems.IsVisible =
                !showEmpty &&
                !_isLoading;

            EmptyStateLayout.IsVisible =
                showEmpty &&
                !_isLoading;
        }


        // =========================================================
        // INCREASE QUANTITY
        // =========================================================

        private void OnIncreaseClicked(
            object? sender,
            EventArgs e)
        {
            if (sender is not Button button)
                return;


            if (button.CommandParameter
                is not CustomerMenuItemModel item)
            {
                return;
            }


            if (!item.IsAvailable)
                return;


            // =====================================================
            // SAFETY LIMIT
            // =====================================================

            const int maximumQuantity = 99;


            if (item.Quantity >= maximumQuantity)
                return;


            item.Quantity++;

            UpdateCartDisplay();
        }


        // =========================================================
        // DECREASE QUANTITY
        // =========================================================

        private void OnDecreaseClicked(
            object? sender,
            EventArgs e)
        {
            if (sender is not Button button)
                return;


            if (button.CommandParameter
                is not CustomerMenuItemModel item)
            {
                return;
            }


            if (item.Quantity <= 0)
                return;


            item.Quantity--;

            UpdateCartDisplay();
        }


        // =========================================================
        // CART DISPLAY
        // =========================================================

        private void UpdateCartDisplay()
        {
            int totalQuantity =
                _allItems.Sum(
                    x =>
                        x.Quantity);


            decimal foodTotal =
                _allItems.Sum(
                    x =>
                        x.Price *
                        x.Quantity);


            LblFoodTotal.Text =
                $"Rs. {foodTotal:N0}";


            LblCartCount.Text =
                totalQuantity.ToString();


            CartBadge.IsVisible =
                totalQuantity > 0;


            BtnContinue.IsEnabled =
                totalQuantity > 0;
        }


        // =========================================================
        // CART TAPPED
        // =========================================================

        private async void OnCartTapped(
            object? sender,
            TappedEventArgs e)
        {
            try
            {
                int totalQuantity =
                    _allItems.Sum(
                        x =>
                            x.Quantity);


                if (totalQuantity <= 0)
                {
                    await DisplayAlertAsync(
                        "Your Cart",
                        "You have not selected any food or drinks yet.",
                        "OK");

                    return;
                }


                decimal foodTotal =
                    _allItems.Sum(
                        x =>
                            x.Price *
                            x.Quantity);


                string summary =
                    string.Join(
                        "\n",
                        _allItems
                            .Where(
                                x =>
                                    x.Quantity > 0)
                            .Select(
                                x =>
                                    $"{x.ItemName} × {x.Quantity}  " +
                                    $"Rs. {(x.Price * x.Quantity):N0}"));


                await DisplayAlertAsync(
                    "Your Food & Drinks",
                    $"{summary}\n\n" +
                    $"Total: Rs. {foodTotal:N0}",
                    "OK");
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(
                    "Cart",
                    $"Unable to display your cart.\n\n{ex.Message}");
            }
        }


        // =========================================================
        // CONTINUE
        // =========================================================

        private async void OnContinueClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                List<CustomerMenuItemModel> selectedItems =
                    _allItems
                        .Where(
                            x =>
                                x.Quantity > 0)
                        .ToList();


                if (selectedItems.Count == 0)
                {
                    await DisplayAlertAsync(
                        "Food & Drinks",
                        "Please select at least one item.",
                        "OK");

                    return;
                }


                decimal foodTotal =
                    selectedItems.Sum(
                        x =>
                            x.Price *
                            x.Quantity);


                // =================================================
                // PHASE 15 — FIRST VERSION
                // =================================================

                string summary =
                    string.Join(
                        "\n",
                        selectedItems.Select(
                            x =>
                                $"{x.ItemName} × {x.Quantity}  " +
                                $"Rs. {(x.Price * x.Quantity):N0}"));


                await DisplayAlertAsync(
                    "Food Selection",
                    $"{summary}\n\n" +
                    $"Food Total: Rs. {foodTotal:N0}\n\n" +
                    "Your food selection is ready. " +
                    "Booking integration will be connected next.",
                    "OK");
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(
                    "Food Selection",
                    $"Unable to continue.\n\n{ex.Message}");
            }
        }


        // =========================================================
        // IMAGE VALIDATION
        // =========================================================

        private static bool HasValidImageUrl(
            string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(
                imageUrl))
            {
                return false;
            }


            // =====================================================
            // LOCAL MAUI IMAGE
            // =====================================================

            if (string.Equals(
                    imageUrl,
                    "arena_bg.jpg",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }


            // =====================================================
            // REMOTE IMAGE
            // =====================================================

            if (!Uri.TryCreate(
                imageUrl,
                UriKind.Absolute,
                out Uri? uri))
            {
                return false;
            }


            return
                uri.Scheme == Uri.UriSchemeHttp ||
                uri.Scheme == Uri.UriSchemeHttps;
        }


        // =========================================================
        // LOADING STATE
        // =========================================================

        private void ShowLoadingState()
        {
            LoadingLayout.IsVisible =
                true;

            LoadingIndicator.IsRunning =
                true;

            CvMenuItems.IsVisible =
                false;

            EmptyStateLayout.IsVisible =
                false;
        }


        private void HideLoadingState()
        {
            LoadingIndicator.IsRunning =
                false;

            LoadingLayout.IsVisible =
                false;

            ApplyFilters();
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
                // Prevent secondary UI exceptions.
            }
        }
    }


    // =============================================================
    // CUSTOMER MENU ITEM MODEL
    // =============================================================

    public class CustomerMenuItemModel :
        INotifyPropertyChanged
    {
        // =========================================================
        // DATABASE / DISPLAY DATA
        // =========================================================

        public int MenuItemID { get; set; }

        public int ArenaID { get; set; }

        public string ItemName { get; set; }
            = string.Empty;

        public decimal Price { get; set; }

        public bool IsAvailable { get; set; }

        public string? ImageUrl { get; set; }

        public string CategoryName { get; set; }
            = "Other";

        public bool HasImage { get; set; }


        // =========================================================
        // QUANTITY
        // =========================================================

        private int _quantity;

        public int Quantity
        {
            get =>
                _quantity;

            set
            {
                if (_quantity == value)
                    return;

                _quantity = value;

                OnPropertyChanged(
                    nameof(Quantity));
            }
        }


        // =========================================================
        // AVAILABILITY
        // =========================================================

        public bool IsUnavailable =>
            !IsAvailable;


        // =========================================================
        // PROPERTY CHANGED
        // =========================================================

        public event PropertyChangedEventHandler?
            PropertyChanged;


        protected virtual void OnPropertyChanged(
            string propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(
                    propertyName));
        }
    }


    // =============================================================
    // CUSTOMER MENU CATEGORY MODEL
    // =============================================================

    public class CustomerMenuCategoryModel
    {
        public string CategoryName { get; set; }
            = string.Empty;


        public bool IsSelected { get; set; }


        public string BackgroundColor { get; set; }
            = "#191B20";


        public string BorderColor { get; set; }
            = "#292D35";


        public string TextColor { get; set; }
            = "#B8BDC6";
    }
}
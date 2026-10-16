using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using IndoPlayHub.UI.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace IndoPlayHub.UI
{
    public partial class CustomerHomePage : ContentPage
    {
        // =========================================================
        // SERVICES
        // =========================================================

        private readonly AppDbContext _dbContext;
        private readonly CustomerSessionService _sessionService;
        private readonly NotificationService _notificationService;


        // =========================================================
        // CUSTOMER
        // =========================================================

        private Customer? _customer;


        // =========================================================
        // SPORTS
        // =========================================================

        private readonly ObservableCollection<CustomerCategoryItem>
            _categories = new();

        private bool _isLoading;


        // =========================================================
        // PUBLIC COLLECTION
        // =========================================================

        public ObservableCollection<CustomerCategoryItem> Categories =>
            _categories;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public CustomerHomePage(
            AppDbContext dbContext,
            CustomerSessionService sessionService,
            NotificationService notificationService)
        {
            InitializeComponent();

            _dbContext = dbContext;
            _sessionService = sessionService;
            _notificationService = notificationService;

            BindingContext = this;
        }


        // =========================================================
        // PAGE APPEARS
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_isLoading)
                return;

            await LoadCustomerHomeAsync();
        }


        // =========================================================
        // LOAD CUSTOMER HOME
        // =========================================================

        private async Task LoadCustomerHomeAsync()
        {
            _isLoading = true;

            try
            {
                // -------------------------------------------------
                // RESTORE CUSTOMER SESSION
                // -------------------------------------------------

                _customer =
                    await _sessionService
                        .RestoreSessionAsync();


                // -------------------------------------------------
                // SESSION NOT FOUND
                // -------------------------------------------------

                if (_customer == null)
                {
                    await DisplayAlertAsync(
                        "Session Expired",
                        "Please login to continue.",
                        "OK");

                    await Shell.Current.GoToAsync(
                        "CustomerLoginPage");

                    return;
                }


                // -------------------------------------------------
                // GREETING
                // -------------------------------------------------

                string firstName =
                    GetFirstName(
                        _customer.FullName);

                LblGreeting.Text =
                    $"Hi, {firstName} 👋";


                // -------------------------------------------------
                // LOAD SPORTS
                // -------------------------------------------------

                await LoadCategoriesAsync();


                // -------------------------------------------------
                // LOAD NOTIFICATION BADGE
                // -------------------------------------------------

                await LoadNotificationBadgeAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"Unable to load the customer home page.\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                _isLoading = false;
            }
        }


        // =========================================================
        // LOAD NOTIFICATION BADGE
        // =========================================================

        private async Task LoadNotificationBadgeAsync()
        {
            try
            {
                if (_customer == null)
                {
                    NotificationBadge.IsVisible = false;
                    return;
                }


                // -------------------------------------------------
                // GET UNREAD NOTIFICATION COUNT
                // -------------------------------------------------

                int unreadCount =
                    await _notificationService
                        .GetUnreadCountAsync(
                            _customer.CustomerID);


                // -------------------------------------------------
                // NO UNREAD NOTIFICATIONS
                // -------------------------------------------------

                if (unreadCount <= 0)
                {
                    NotificationBadge.IsVisible = false;
                    LblNotificationCount.Text = "0";

                    return;
                }


                // -------------------------------------------------
                // SHOW BADGE
                // -------------------------------------------------

                NotificationBadge.IsVisible = true;


                // -------------------------------------------------
                // DISPLAY COUNT
                // -------------------------------------------------

                if (unreadCount > 99)
                {
                    LblNotificationCount.Text = "99+";
                }
                else
                {
                    LblNotificationCount.Text =
                        unreadCount.ToString();
                }
            }
            catch
            {
                // -------------------------------------------------
                // Notification badge should never break
                // the customer home page.
                // -------------------------------------------------

                NotificationBadge.IsVisible = false;
            }
        }


        // =========================================================
        // NOTIFICATIONS
        // =========================================================

        private async void OnNotificationsTapped(
            object? sender,
            TappedEventArgs e)
        {
            try
            {
                // -------------------------------------------------
                // MAKE SURE CUSTOMER SESSION EXISTS
                // -------------------------------------------------

                if (_customer == null)
                {
                    _customer =
                        await _sessionService
                            .RestoreSessionAsync();
                }


                // -------------------------------------------------
                // SESSION EXPIRED
                // -------------------------------------------------

                if (_customer == null)
                {
                    await DisplayAlertAsync(
                        "Session Expired",
                        "Please login to continue.",
                        "OK");

                    await Shell.Current.GoToAsync(
                        "CustomerLoginPage");

                    return;
                }


                // -------------------------------------------------
                // OPEN NOTIFICATIONS
                // -------------------------------------------------

                await Shell.Current.GoToAsync(
                    "CustomerNotificationsPage");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Notifications",
                    $"Unable to open notifications.\n\n{ex.Message}",
                    "OK");
            }
        }


        // =========================================================
        // LOAD SPORTS FROM DATABASE
        // =========================================================

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var categories =
                    await _dbContext.Categories
                        .AsNoTracking()
                        .OrderBy(c => c.CategoryID)
                        .ToListAsync();

                _categories.Clear();

                foreach (var category in categories)
                {
                    string categoryName =
                        GetCategoryName(category);

                    _categories.Add(
                        new CustomerCategoryItem
                        {
                            CategoryID =
                                category.CategoryID,

                            CategoryName =
                                categoryName,

                            ImageSource =
                                GetSportImage(categoryName)
                        });
                }


                // -------------------------------------------------
                // DISPLAY SPORTS
                // -------------------------------------------------

                CvSports.ItemsSource =
                    _categories;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Sports Error",
                    $"Unable to load sports categories.\n\n{ex.Message}",
                    "OK");
            }
        }


        // =========================================================
        // CATEGORY NAME
        // =========================================================

        private static string GetCategoryName(
            Category category)
        {
            return category.CategoryName?.Trim()
                   ?? "Sport";
        }


        // =========================================================
        // SPORT IMAGE MAPPING
        // =========================================================

        private static string GetSportImage(
            string categoryName)
        {
            string name =
                categoryName
                    .Trim()
                    .ToLowerInvariant();

            return name switch
            {
                "indoor futsal" =>
                    "futsal.jpg",

                "indoor cricket" =>
                    "cricket.jpg",

                "badminton" =>
                    "badminton.jpg",

                "table tennis" =>
                    "tabletennis.jpg",

                "basketball" =>
                    "basketball.jpg",

                "squash" =>
                    "squash.jpg",

                "indoor volleyball" =>
                    "volleyball.jpg",

                "billiards & pool" =>
                    "billiardsandpool.jpg",

                "indoor boxing" =>
                    "boxing.jpg",

                "fitness & gym" =>
                    "fitnessandgym.jpg",

                "snooker room" =>
                    "snooker.jpg",

                "yoga & pilates" =>
                    "yogaandpilates.jpg",

                "indoor board games" =>
                    "boardgames.jpg",

                "indoor climbing" =>
                    "climbing.jpg",

                "cricket" =>
                    "cricket.jpg",

                "martial arts dojo" =>
                    "martialartsdojo.jpg",

                "indoor football" =>
                    "football.jpg",

                "soccer" =>
                    "indoorsoccer.jpg",

                _ =>
                    "arena_bg.jpg"
            };
        }


        // =========================================================
        // GET FIRST NAME
        // =========================================================

        private static string GetFirstName(
            string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "Player";

            string[] parts =
                fullName
                    .Trim()
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries);

            return parts.Length > 0
                ? parts[0]
                : "Player";
        }


        // =========================================================
        // SEARCH SPORTS
        // =========================================================

        private void OnSearchTextChanged(
            object? sender,
            TextChangedEventArgs e)
        {
            string search =
                e.NewTextValue?
                    .Trim()
                    .ToLowerInvariant()
                ?? string.Empty;


            // -----------------------------------------------------
            // SHOW ALL SPORTS
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(search))
            {
                CvSports.ItemsSource =
                    _categories;

                return;
            }


            // -----------------------------------------------------
            // FILTER SPORTS
            // -----------------------------------------------------

            CvSports.ItemsSource =
                _categories
                    .Where(c =>
                        c.CategoryName
                            .ToLowerInvariant()
                            .Contains(search))
                    .ToList();
        }


        // =========================================================
        // SPORT CLICK
        // =========================================================

        private async void OnSportTapped(
            object? sender,
            TappedEventArgs e)
        {
            try
            {
                if (sender is not Grid grid)
                    return;

                if (grid.BindingContext
                    is not CustomerCategoryItem category)
                {
                    return;
                }


                // -------------------------------------------------
                // PHASE 4
                // SPORT → ARENAS
                // -------------------------------------------------

                await Shell.Current.GoToAsync(
                    "CustomerSportArenasPage",
                    new Dictionary<string, object>
                    {
                        ["CategoryID"] =
                            category.CategoryID,

                        ["CategoryName"] =
                            category.CategoryName
                    });
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Sport Selection Error",
                    $"Unable to open this sport.\n\n{ex.Message}",
                    "OK");
            }
        }


        // =========================================================
        // LOCATION
        // =========================================================

        private async void OnLocationTapped(
            object? sender,
            TappedEventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync(
                    "CustomerLocationPage");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Location",
                    $"Unable to open location search.\n\n{ex.Message}",
                    "OK");
            }
        }


        // =========================================================
        // PHASE 11 — MY BOOKINGS
        // =========================================================

        private async void OnMyBookingsTapped(
            object? sender,
            TappedEventArgs e)
        {
            try
            {
                // -------------------------------------------------
                // MAKE SURE CUSTOMER SESSION EXISTS
                // -------------------------------------------------

                if (_customer == null)
                {
                    _customer =
                        await _sessionService
                            .RestoreSessionAsync();
                }


                // -------------------------------------------------
                // SESSION EXPIRED
                // -------------------------------------------------

                if (_customer == null)
                {
                    await DisplayAlertAsync(
                        "Session Expired",
                        "Please login to continue.",
                        "OK");

                    await Shell.Current.GoToAsync(
                        "CustomerLoginPage");

                    return;
                }


                // -------------------------------------------------
                // OPEN MY BOOKINGS
                // -------------------------------------------------

                await Shell.Current.GoToAsync(
                    "CustomerMyBookingsPage");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "My Bookings",
                    $"Unable to open your bookings.\n\n{ex.Message}",
                    "OK");
            }
        }


        // =========================================================
        // PROFILE
        // =========================================================

        private async void OnProfileTapped(
            object? sender,
            TappedEventArgs e)
        {
            try
            {
                if (_customer == null)
                    return;

                await Navigation.PushAsync(
                    new CustomerProfilePage(
                        _dbContext,
                        _customer.CustomerID));
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Profile",
                    $"Unable to open your profile.\n\n{ex.Message}",
                    "OK");
            }
        }
    }


    // =============================================================
    // CUSTOMER CATEGORY DISPLAY MODEL
    // =============================================================

    public class CustomerCategoryItem
    {
        public int CategoryID { get; set; }

        public string CategoryName { get; set; } =
            string.Empty;

        public string ImageSource { get; set; } =
            "arena_bg.jpg";
    }
}
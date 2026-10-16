using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using Microsoft.EntityFrameworkCore;

namespace IndoPlayHub.UI
{
    public partial class CustomerLocationPage : ContentPage
    {
        private readonly AppDbContext _dbContext;

        private List<CustomerArenaItem> _allArenas = new();

        private bool _isLoading;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public CustomerLocationPage(
            AppDbContext dbContext)
        {
            InitializeComponent();

            _dbContext = dbContext;
        }


        // =========================================================
        // PAGE APPEARS
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_isLoading)
                return;

            await LoadArenasAsync();
        }


        // =========================================================
        // LOAD ARENAS
        // =========================================================

        private async Task LoadArenasAsync()
        {
            _isLoading = true;

            try
            {
                var arenas =
                    await _dbContext.Arenas
                        .AsNoTracking()
                        .Where(a =>
                            a.IsSetupCompleted)
                        .OrderBy(a =>
                            a.ArenaName)
                        .ToListAsync();


                _allArenas =
                    arenas
                        .Select(CreateArenaItem)
                        .ToList();


                CvArenas.ItemsSource =
                    _allArenas;


                EmptyState.IsVisible =
                    _allArenas.Count == 0;


                if (_allArenas.Count == 0)
                {
                    LblArenaHeading.Text =
                        "Available Arenas";

                    LblArenaSubheading.Text =
                        "No arenas are currently available.";

                    LblEmptyMessage.Text =
                        "Arena owners need to complete their setup first.";
                }
                else
                {
                    LblArenaHeading.Text =
                        "All Available Arenas";

                    LblArenaSubheading.Text =
                        $"{_allArenas.Count} arena(s) available.";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Arena Error",
                    $"Unable to load arenas.\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                _isLoading = false;
            }
        }


        // =========================================================
        // CREATE CUSTOMER DISPLAY MODEL
        // =========================================================

        private static CustomerArenaItem CreateArenaItem(
            Arena arena)
        {
            return new CustomerArenaItem
            {
                ArenaID =
                    arena.ArenaID,

                ArenaName =
                    arena.ArenaName,

                Location =
                    arena.Location,

                OpeningHours =
                    $"Open {FormatTime(arena.OpeningTime)} - " +
                    $"{FormatTime(arena.ClosingTime)}",

                ImageSource =
                    GetArenaImage(arena)
            };
        }


        // =========================================================
        // ARENA IMAGE
        // =========================================================

        private static string GetArenaImage(
            Arena arena)
        {
            if (!string.IsNullOrWhiteSpace(
                    arena.CoverImageUrl))
            {
                string image =
                    arena.CoverImageUrl.Trim();

                // Local MAUI image
                // Example: arena_bg.jpg
                if (!image.Contains("://"))
                {
                    return image;
                }

                // Remote image URL
                return image;
            }

            return "arena_bg.jpg";
        }


        // =========================================================
        // FORMAT TIME
        // =========================================================

        private static string FormatTime(
            TimeSpan time)
        {
            return DateTime.Today
                .Add(time)
                .ToString("h:mm tt");
        }


        // =========================================================
        // CITY SELECTION
        // =========================================================

        private async void OnCityClicked(
            object? sender,
            EventArgs e)
        {
            if (sender is not Button button)
                return;

            string city =
                button.CommandParameter?.ToString()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(city))
                return;

            await ShowArenasForLocationAsync(city);
        }


        // =========================================================
        // FILTER ARENAS
        // =========================================================

        private async Task ShowArenasForLocationAsync(
            string location)
        {
            try
            {
                string normalizedLocation =
                    location.Trim();


                var filtered =
                    _allArenas
                        .Where(a =>
                            a.Location.Contains(
                                normalizedLocation,
                                StringComparison.OrdinalIgnoreCase))
                        .ToList();


                CvArenas.ItemsSource =
                    filtered;


                LocationSummary.IsVisible =
                    true;


                LblSelectedLocation.Text =
                    normalizedLocation;


                LblArenaHeading.Text =
                    $"Arenas in {normalizedLocation}";


                LblArenaSubheading.Text =
                    filtered.Count == 0
                        ? "No registered arenas were found in this location."
                        : $"{filtered.Count} arena(s) found.";


                EmptyState.IsVisible =
                    filtered.Count == 0;


                LblEmptyMessage.Text =
                    $"There are currently no registered arenas in {normalizedLocation}.";
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Location Error",
                    $"Unable to search this location.\n\n{ex.Message}",
                    "OK");
            }
        }


        // =========================================================
        // CURRENT LOCATION
        // =========================================================

        private async void OnCurrentLocationClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                LblCurrentLocation.Text =
                    "Detecting your location...";


                Location? location =
                    await Geolocation.Default
                        .GetLocationAsync(
                            new GeolocationRequest(
                                GeolocationAccuracy.Medium,
                                TimeSpan.FromSeconds(10)));


                if (location == null)
                {
                    LblCurrentLocation.Text =
                        "Location could not be detected.";

                    await DisplayAlertAsync(
                        "Location Unavailable",
                        "Your current location could not be detected. Please select a city instead.",
                        "OK");

                    return;
                }


                var placemarks =
                    await Geocoding.Default
                        .GetPlacemarksAsync(
                            location.Latitude,
                            location.Longitude);


                var placemark =
                    placemarks?.FirstOrDefault();


                string? city =
                    placemark?.Locality
                    ?? placemark?.SubAdminArea;


                if (string.IsNullOrWhiteSpace(city))
                {
                    LblCurrentLocation.Text =
                        "Location detected";

                    await DisplayAlertAsync(
                        "City Not Found",
                        "Your location was detected, but the city could not be determined. Please select a city manually.",
                        "OK");

                    return;
                }


                LblCurrentLocation.Text =
                    city;


                await ShowArenasForLocationAsync(city);
            }
            catch (FeatureNotEnabledException)
            {
                LblCurrentLocation.Text =
                    "Location services are disabled.";

                await DisplayAlertAsync(
                    "Location Disabled",
                    "Please enable location services on your device and try again.",
                    "OK");
            }
            catch (PermissionException)
            {
                LblCurrentLocation.Text =
                    "Location permission denied.";

                await DisplayAlertAsync(
                    "Permission Required",
                    "Please allow location access for IndoPlayHub, or select a city manually.",
                    "OK");
            }
            catch (Exception ex)
            {
                LblCurrentLocation.Text =
                    "Unable to detect location.";

                await DisplayAlertAsync(
                    "Location Error",
                    $"Unable to determine your current location.\n\n{ex.Message}",
                    "OK");
            }
        }


        // =========================================================
        // ARENA CLICK
        // =========================================================

        // =========================================================
        // ARENA CLICK
        // =========================================================

        private async void OnArenaClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                // -----------------------------------------------------
                // GET SELECTED ARENA
                // -----------------------------------------------------

                if (sender is not Button button)
                    return;


                if (button.CommandParameter
                    is not CustomerArenaItem arena)
                {
                    return;
                }


                // -----------------------------------------------------
                // VALIDATE ARENA
                // -----------------------------------------------------

                if (arena.ArenaID <= 0)
                {
                    await DisplayAlertAsync(
                        "Arena",
                        "Invalid arena selected.",
                        "OK");

                    return;
                }


                // -----------------------------------------------------
                // OPEN COMPLETE ARENA DETAILS
                // -----------------------------------------------------
                //
                // IMPORTANT:
                //
                // We intentionally DO NOT send CategoryID here.
                //
                // This means CustomerArenaDetailsPage will know that
                // the customer entered through:
                //
                // Find Arenas → City → View Arena
                //
                // and should therefore display ALL sports/courts
                // registered for this arena.
                //
                // -----------------------------------------------------

                await Shell.Current.GoToAsync(
                    "CustomerArenaDetailsPage",
                    new Dictionary<string, object>
                    {
                        ["ArenaID"] = arena.ArenaID
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
        // CLOSE
        // =========================================================

        private async void OnCloseTapped(
            object? sender,
            TappedEventArgs e)
        {
            try
            {
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Navigation Error",
                    $"Unable to close this page.\n\n{ex.Message}",
                    "OK");
            }
        }
    }


    // =============================================================
    // CUSTOMER ARENA DISPLAY MODEL
    // =============================================================

    public class CustomerArenaItem
    {
        public int ArenaID { get; set; }

        public string ArenaName { get; set; } =
            string.Empty;

        public string Location { get; set; } =
            string.Empty;

        public string OpeningHours { get; set; } =
            string.Empty;

        public string ImageSource { get; set; } =
            "arena_bg.jpg";
    }
}
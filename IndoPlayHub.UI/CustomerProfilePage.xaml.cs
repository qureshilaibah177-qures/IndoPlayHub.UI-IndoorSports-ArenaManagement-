using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using Microsoft.EntityFrameworkCore;

namespace IndoPlayHub.UI
{
    public partial class CustomerProfilePage : ContentPage
    {
        private readonly AppDbContext _dbContext;
        private readonly int _customerId;


    private Customer? _customer;

        public CustomerProfilePage(
            AppDbContext dbContext,
            int customerId)
        {
            InitializeComponent();

            _dbContext = dbContext;
            _customerId = customerId;
        }

        // =========================================================
        // PAGE APPEARS
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await LoadCustomerAsync();
        }

        // =========================================================
        // LOAD CUSTOMER
        // =========================================================

        private async Task LoadCustomerAsync()
        {
            try
            {
                _customer = await _dbContext.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c =>
                        c.CustomerID == _customerId);

                if (_customer == null)
                {
                    await DisplayAlertAsync(
                        "Account Not Found",
                        "Your customer account could not be found.",
                        "OK");

                    await Navigation.PopAsync();

                    return;
                }

                // -------------------------------------------------
                // BASIC INFORMATION
                // -------------------------------------------------

                LblFullName.Text =
                    _customer.FullName;

                LblEmail.Text =
                    _customer.Email;

                LblPhone.Text =
                    _customer.PhoneNumber;

                LblCreatedAt.Text =
                    _customer.CreatedAt.ToString("MMMM yyyy");

                // -------------------------------------------------
                // ACCOUNT STATUS
                // -------------------------------------------------

                if (_customer.IsActive)
                {
                    LblAccountStatus.Text =
                        "Active Account";

                    LblAccountStatus.TextColor =
                        Color.FromArgb("#03DAC6");
                }
                else
                {
                    LblAccountStatus.Text =
                        "Inactive Account";

                    LblAccountStatus.TextColor =
                        Color.FromArgb("#CF6679");
                }

                // -------------------------------------------------
                // PROFILE IMAGE
                // -------------------------------------------------

                if (!string.IsNullOrWhiteSpace(
                        _customer.ProfileImageUrl))
                {
                    ImgProfile.Source =
                        _customer.ProfileImageUrl;
                }
                else
                {
                    ImgProfile.Source =
                        "profile_placeholder.png";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"Failed to load your account.\n\n{ex.Message}",
                    "OK");
            }
        }

        // =========================================================
        // EDIT PROFILE
        // =========================================================

        private async void OnEditProfileClicked(
            object? sender,
            EventArgs e)
        {
            if (_customer == null)
                return;

            await Navigation.PushAsync(
                new EditCustomerProfilePage(
                    _dbContext,
                    _customer.CustomerID));
        }

        // =========================================================
        // CHANGE PASSWORD
        // =========================================================

        private async void OnChangePasswordClicked(
            object? sender,
            EventArgs e)
        {
            if (_customer == null)
                return;

            await Navigation.PushAsync(
                new ChangeCustomerPasswordPage(
                    _dbContext,
                    _customer.CustomerID));
        }

        // =========================================================
        // MY BOOKINGS
        // =========================================================

        private async void OnMyBookingsClicked(
            object? sender,
            EventArgs e)
        {
            await DisplayAlertAsync(
                "My Bookings",
                "Your booking history will be available here.",
                "OK");
        }

        // =========================================================
        // FAVORITES
        // =========================================================

        private async void OnFavoritesClicked(
            object? sender,
            EventArgs e)
        {
            await DisplayAlertAsync(
                "Favorites",
                "Your favorite arenas will be available here.",
                "OK");
        }

        // =========================================================
        // REVIEWS
        // =========================================================

        private async void OnReviewsClicked(
            object? sender,
            EventArgs e)
        {
            await DisplayAlertAsync(
                "Reviews",
                "Your arena reviews will be available here.",
                "OK");
        }

        // =========================================================
        // PAYMENT METHODS
        // =========================================================

        private async void OnPaymentMethodsClicked(
            object? sender,
            EventArgs e)
        {
            await DisplayAlertAsync(
                "Payment Methods",
                "Payment methods will be available here.",
                "OK");
        }

        // =========================================================
        // NOTIFICATIONS
        // =========================================================

        private async void OnNotificationsClicked(
            object? sender,
            EventArgs e)
        {
            await DisplayAlertAsync(
                "Notifications",
                "Your customer notifications will be available here.",
                "OK");
        }

        // =========================================================
        // HELP & SUPPORT
        // =========================================================

        private async void OnHelpSupportClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(
                    new CustomerSupportPage());
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Help & Support",
                    $"Unable to open Help & Support.\n\n{ex.Message}",
                    "OK");
            }
        }

        // =========================================================
        // TERMS & CONDITIONS
        // =========================================================

        private async void OnTermsClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(
                    new CustomerTermsPage());
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Terms & Conditions",
                    $"Unable to open Terms & Conditions.\n\n{ex.Message}",
                    "OK");
            }
        }

        // =========================================================
        // PRIVACY POLICY
        // =========================================================

        private async void OnPrivacyClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(
                    new CustomerPrivacyPage());
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Privacy Policy",
                    $"Unable to open Privacy Policy.\n\n{ex.Message}",
                    "OK");
            }
        }

        // =========================================================
        // LOGOUT
        // =========================================================

        private async void OnLogoutClicked(
            object? sender,
            EventArgs e)
        {
            bool confirm =
                await DisplayAlertAsync(
                    "Logout",
                    "Are you sure you want to logout?",
                    "Logout",
                    "Cancel");

            if (!confirm)
                return;

            Preferences.Remove("CustomerID");
            Preferences.Remove("CustomerLoggedIn");

            await Shell.Current.GoToAsync(
                "CustomerLoginPage");
        }
    }


}

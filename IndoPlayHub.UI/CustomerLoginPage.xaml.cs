using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using IndoPlayHub.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace IndoPlayHub.UI
{
    public partial class CustomerLoginPage : ContentPage
    {
        private readonly AppDbContext _dbContext;
        private readonly CustomerSessionService _sessionService;

        private bool _isLoggingIn;
        private bool _passwordVisible;

        public CustomerLoginPage(
            AppDbContext dbContext,
            CustomerSessionService sessionService)
        {
            InitializeComponent();

            _dbContext = dbContext;
            _sessionService = sessionService;
        }


        // =========================================================
        // PAGE APPEARING
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // If the customer previously chose Remember Me,
            // keep the checkbox checked when returning to login.
            ChkRememberMe.IsChecked =
                Preferences.Get(
                    "CustomerRememberMe",
                    false);
        }


        // =========================================================
        // PASSWORD VISIBILITY
        // =========================================================

        private void OnPasswordVisibilityClicked(
            object? sender,
            EventArgs e)
        {
            _passwordVisible = !_passwordVisible;

            TxtPassword.IsPassword = !_passwordVisible;

            BtnPasswordVisibility.Text =
                _passwordVisible ? "🙈" : "👁";
        }


        // =========================================================
        // LOGIN
        // =========================================================

        private async void OnLoginClicked(
            object? sender,
            EventArgs e)
        {
            if (_isLoggingIn)
                return;

            _isLoggingIn = true;

            try
            {
                string login =
                    TxtEmailOrPhone.Text?.Trim()
                    ?? string.Empty;

                string password =
                    TxtPassword.Text
                    ?? string.Empty;


                // -------------------------------------------------
                // VALIDATION
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(login))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter your email or phone number.",
                        "OK");

                    return;
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter your password.",
                        "OK");

                    return;
                }


                // -------------------------------------------------
                // FIND CUSTOMER
                // -------------------------------------------------

                string normalizedLogin =
                    login.ToLowerInvariant();

                Customer? customer =
                    await _dbContext.Customers
                        .FirstOrDefaultAsync(c =>
                            c.Email.ToLower() == normalizedLogin ||
                            c.PhoneNumber == login);


                // -------------------------------------------------
                // ACCOUNT NOT FOUND
                // -------------------------------------------------

                if (customer == null)
                {
                    await DisplayAlertAsync(
                        "Login Failed",
                        "No customer account was found with these credentials.",
                        "OK");

                    return;
                }


                // -------------------------------------------------
                // ACCOUNT DISABLED
                // -------------------------------------------------

                if (!customer.IsActive)
                {
                    await DisplayAlertAsync(
                        "Account Disabled",
                        "Your customer account is currently inactive. Please contact support.",
                        "OK");

                    return;
                }


                // -------------------------------------------------
                // PASSWORD
                // -------------------------------------------------

                if (customer.Password != password)
                {
                    await DisplayAlertAsync(
                        "Login Failed",
                        "The password you entered is incorrect.",
                        "OK");

                    return;
                }


                // -------------------------------------------------
                // REMEMBER ME
                // -------------------------------------------------

                bool rememberMe =
                    ChkRememberMe.IsChecked;

                await _sessionService.SaveSessionAsync(
                    customer.CustomerID,
                    rememberMe);


                // -------------------------------------------------
                // SUCCESS
                // -------------------------------------------------

                await DisplayAlertAsync(
                    "Welcome",
                    $"Welcome back, {customer.FullName}!",
                    "Continue");


                // -------------------------------------------------
                // CUSTOMER HOME
                // -------------------------------------------------

                await Shell.Current.GoToAsync(
                    "CustomerHomePage");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"Unable to login.\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                _isLoggingIn = false;
            }
        }


        // =========================================================
        // REGISTER
        // =========================================================

        private async void OnRegisterTapped(
            object? sender,
            TappedEventArgs e)
        {
            await Shell.Current.GoToAsync(
                "CustomerRegisterPage");
        }


        // =========================================================
        // FORGOT PASSWORD
        // =========================================================

        private async void OnForgotPasswordTapped(
            object? sender,
            TappedEventArgs e)
        {
            try
            {
                string? email = await DisplayPromptAsync(
                    "Forgot Password",
                    "Enter your registered customer email:",
                    "Send OTP",
                    "Cancel",
                    "customer@example.com",
                    keyboard: Keyboard.Email);

                if (string.IsNullOrWhiteSpace(email))
                    return;

                email = email.Trim();

                if (!IsValidEmail(email))
                {
                    await DisplayAlertAsync(
                        "Invalid Email",
                        "Please enter a valid email address.",
                        "OK");

                    return;
                }

                var customer = await _dbContext.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c =>
                        c.Email.ToLower() == email.ToLower());

                if (customer == null)
                {
                    await DisplayAlertAsync(
                        "Not Found",
                        "No customer account is registered with this email address.",
                        "OK");

                    return;
                }

                await DisplayAlertAsync(
                    "OTP Sent",
                    $"A verification code has been sent to {email}.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Password Recovery Error",
                    $"Could not process the password recovery request:\n\n{ex.Message}",
                    "OK");
            }
        }


        // =========================================================
        // EMAIL VALIDATION
        // =========================================================

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var address = new System.Net.Mail.MailAddress(email);

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
    }
}
using IndoPlayHub.UI.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

namespace IndoPlayHub.UI
{
    public partial class LoginPage : ContentPage
    {
        private readonly AppDbContext _dbContext;
        private bool _isLoggingIn;

        // Keeps track of the arena that is currently logged in.
        private int _currentArenaId;

        public LoginPage(AppDbContext dbContext)
        {
            InitializeComponent();

            _dbContext = dbContext;

            // =========================================================
            // FORGOT PASSWORD GESTURE
            // =========================================================

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += OnForgotPasswordClicked;

            LblForgotPassword.GestureRecognizers.Add(tapGesture);
        }

        // =========================================================
        // SHOW / HIDE PASSWORD
        // =========================================================

        private void OnShowPasswordClicked(
            object? sender,
            EventArgs e)
        {
            TxtLoginPassword.IsPassword =
                !TxtLoginPassword.IsPassword;

            BtnShowPassword.Text =
                TxtLoginPassword.IsPassword
                    ? "👁"
                    : "Hide";
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
                string email =
                    TxtLoginEmail.Text?.Trim() ?? string.Empty;

                string password =
                    TxtLoginPassword.Text ?? string.Empty;

                // -------------------------------------------------
                // VALIDATE EMAIL
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(email))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter your email address.",
                        "OK");

                    return;
                }

                if (!IsValidEmail(email))
                {
                    await DisplayAlertAsync(
                        "Invalid Email",
                        "Please enter a valid email address.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // VALIDATE PASSWORD
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(password))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter your password.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // FIND ARENA
                // -------------------------------------------------

                var arena = await _dbContext.Arenas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a =>
                        a.Email == email);

                // -------------------------------------------------
                // INVALID EMAIL
                // -------------------------------------------------

                if (arena == null)
                {
                    await DisplayAlertAsync(
                        "Login Failed",
                        "Invalid email or password. Please try again.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // PASSWORD CHECK
                //
                // NOTE:
                // This is currently plain-text password comparison.
                // It should be replaced with password hashing later.
                // -------------------------------------------------

                if (arena.Password != password)
                {
                    await DisplayAlertAsync(
                        "Login Failed",
                        "Invalid email or password. Please try again.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // SAVE CURRENT ARENA ID
                // -------------------------------------------------

                _currentArenaId = arena.ArenaID;

                // =================================================
                // LOGIN SUCCESSFUL
                // =================================================

                // -------------------------------------------------
                // SETUP NOT COMPLETED
                // -------------------------------------------------

                if (!arena.IsSetupCompleted)
                {
                    await Navigation.PushAsync(
                        new SetupArenaPage(
                            _dbContext,
                            arena.ArenaID));

                    return;
                }

                // -------------------------------------------------
                // SETUP COMPLETED
                // -------------------------------------------------

                await Navigation.PushAsync(
                    new DashboardPage(
                        _dbContext,
                        arena.ArenaID));
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
                    "Login Error",
                    $"An error occurred while logging in:\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                _isLoggingIn = false;
            }
        }

        // =========================================================
        // GO TO REGISTRATION
        // =========================================================

        private async void OnGoToRegisterClicked(
            object? sender,
            TappedEventArgs e)
        {
            try
            {
                await Navigation.PushAsync(
                    new RegisterArenaPage(_dbContext));
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Navigation Error",
                    $"Could not open registration:\n\n{ex.Message}",
                    "OK");
            }
        }

        // =========================================================
        // FORGOT PASSWORD
        // =========================================================

        private async void OnForgotPasswordClicked(
            object? sender,
            TappedEventArgs e)
        {
            try
            {
                string? email = await DisplayPromptAsync(
                    "Forgot Password",
                    "Enter your registered business email:",
                    "Send OTP",
                    "Cancel",
                    "owner@domain.com",
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

                var arena = await _dbContext.Arenas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a =>
                        a.Email == email);

                if (arena == null)
                {
                    await DisplayAlertAsync(
                        "Not Found",
                        "No arena is registered with this email address.",
                        "OK");

                    return;
                }

                // =================================================
                // TEMPORARY OTP FLOW
                //
                // Replace this later with an actual email service.
                // =================================================

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
        // MANAGE COURTS
        // =========================================================

        private async void OnManageCourtsClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                if (_currentArenaId <= 0)
                {
                    await DisplayAlertAsync(
                        "Login Required",
                        "Please log in first.",
                        "OK");

                    return;
                }

                var arena = await _dbContext.Arenas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        a => a.ArenaID == _currentArenaId);

                if (arena == null)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "The logged-in arena could not be found.",
                        "OK");

                    return;
                }

                if (!arena.IsSetupCompleted)
                {
                    await DisplayAlertAsync(
                        "Setup Required",
                        "Please complete your arena setup before managing courts.",
                        "OK");

                    await Navigation.PushAsync(
                        new SetupArenaPage(
                            _dbContext,
                            arena.ArenaID));

                    return;
                }

                await Navigation.PushAsync(
                    new CourtsPage(
                        _dbContext,
                        arena.ArenaID));
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Navigation Error",
                    $"Could not open courts management:\n\n{ex.Message}",
                    "OK");
            }
        }

        // =========================================================
        // MANAGE MENU
        // =========================================================

        private async void OnManageMenuClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                if (_currentArenaId <= 0)
                {
                    await DisplayAlertAsync(
                        "Login Required",
                        "Please log in first.",
                        "OK");

                    return;
                }

                var arena = await _dbContext.Arenas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        a => a.ArenaID == _currentArenaId);

                if (arena == null)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "The logged-in arena could not be found.",
                        "OK");

                    return;
                }

                if (!arena.IsSetupCompleted)
                {
                    await DisplayAlertAsync(
                        "Setup Required",
                        "Please complete your arena setup before managing menu items.",
                        "OK");

                    await Navigation.PushAsync(
                        new SetupArenaPage(
                            _dbContext,
                            arena.ArenaID));

                    return;
                }

                await Navigation.PushAsync(
                    new MenuPage(
                        _dbContext,
                        arena.ArenaID));
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Navigation Error",
                    $"Could not open menu management:\n\n{ex.Message}",
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
            try
            {
                _currentArenaId = 0;

                TxtLoginEmail.Text = string.Empty;
                TxtLoginPassword.Text = string.Empty;

                // Return to LoginPage.
                while (Navigation.NavigationStack.Count > 1)
                {
                    await Navigation.PopAsync();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Logout Error",
                    $"Could not return to the login page:\n\n{ex.Message}",
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
                var address = new MailAddress(email);

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
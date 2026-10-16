using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using Microsoft.EntityFrameworkCore;

namespace IndoPlayHub.UI
{
    public partial class RegisterArenaPage : ContentPage
    {
        private readonly AppDbContext _dbContext;
        private bool _isRegistering = false;

        public RegisterArenaPage(AppDbContext dbContext)
        {
            InitializeComponent();

            _dbContext = dbContext;
        }

        // =========================================================
        // REGISTER ARENA
        // =========================================================

        private async void OnRegisterClicked(
            object? sender,
            EventArgs e)
        {
            if (_isRegistering)
                return;

            _isRegistering = true;

            try
            {
                // -------------------------------------------------
                // READ VALUES
                // -------------------------------------------------

                string ownerName =
                    TxtOwnerName.Text?.Trim() ?? string.Empty;

                string ownerCNIC =
                    TxtOwnerCNIC.Text?.Trim() ?? string.Empty;

                string arenaName =
                    TxtArenaName.Text?.Trim() ?? string.Empty;

                string location =
                    TxtLocation.Text?.Trim() ?? string.Empty;

                string email =
                    TxtEmail.Text?.Trim() ?? string.Empty;

                string phone =
                    TxtPhone.Text?.Trim() ?? string.Empty;

                string password =
                    TxtPassword.Text ?? string.Empty;

                string confirmPassword =
                    TxtConfirmPassword.Text ?? string.Empty;

                string coverImage =
                    TxtCoverImage.Text?.Trim() ?? string.Empty;

                // -------------------------------------------------
                // BASIC VALIDATION
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(ownerName))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter the owner name.",
                        "OK");

                    return;
                }

                if (string.IsNullOrWhiteSpace(ownerCNIC))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter the owner CNIC.",
                        "OK");

                    return;
                }

                if (string.IsNullOrWhiteSpace(arenaName))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter the arena/business name.",
                        "OK");

                    return;
                }

                if (string.IsNullOrWhiteSpace(location))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter the arena location.",
                        "OK");

                    return;
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter the business email.",
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

                if (string.IsNullOrWhiteSpace(phone))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter the contact number.",
                        "OK");

                    return;
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter a password.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // PASSWORD VALIDATION
                // -------------------------------------------------

                if (password.Length < 6)
                {
                    await DisplayAlertAsync(
                        "Weak Password",
                        "Password must contain at least 6 characters.",
                        "OK");

                    return;
                }

                if (password != confirmPassword)
                {
                    await DisplayAlertAsync(
                        "Password Mismatch",
                        "Passwords do not match. Please check again.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // OWNER ID
                //
                // If your database requires OwnerID and you enter it
                // manually, use the value from TxtOwnerID.
                //
                // Otherwise your existing database design uses 1
                // as the default owner.
                // -------------------------------------------------

                int ownerId = 1;

                if (!string.IsNullOrWhiteSpace(TxtOwnerID.Text))
                {
                    if (!int.TryParse(
                            TxtOwnerID.Text.Trim(),
                            out ownerId) ||
                        ownerId <= 0)
                    {
                        await DisplayAlertAsync(
                            "Invalid Owner ID",
                            "Please enter a valid numeric Owner ID.",
                            "OK");

                        return;
                    }
                }

                // -------------------------------------------------
                // CHECK DUPLICATE EMAIL
                // -------------------------------------------------

                bool emailAlreadyExists =
                    await _dbContext.Arenas
                        .AnyAsync(a => a.Email == email);

                if (emailAlreadyExists)
                {
                    await DisplayAlertAsync(
                        "Email Already Registered",
                        "An arena is already registered with this email address.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // CHECK DUPLICATE CNIC
                // -------------------------------------------------

                bool cnicAlreadyExists =
                    await _dbContext.Arenas
                        .AnyAsync(a => a.OwnerCNIC == ownerCNIC);

                if (cnicAlreadyExists)
                {
                    await DisplayAlertAsync(
                        "CNIC Already Registered",
                        "An arena owner with this CNIC is already registered.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // CREATE ARENA
                // -------------------------------------------------

                var newArena = new Arena
                {
                    OwnerID = ownerId,

                    OwnerName =
                        ownerName,

                    OwnerCNIC =
                        ownerCNIC,

                    ArenaName =
                        arenaName,

                    Location =
                        location,

                    Email =
                        email,

                    Password =
                        password,

                    PhoneNumber =
                        phone,

                    OpeningTime =
                        new TimeSpan(8, 0, 0),

                    ClosingTime =
                        new TimeSpan(23, 0, 0),

                    CoverImageUrl =
                        string.IsNullOrWhiteSpace(coverImage)
                            ? null
                            : coverImage,

                    CreatedAt =
                        DateTime.Now
                };

                // -------------------------------------------------
                // SAVE TO DATABASE
                // -------------------------------------------------

                _dbContext.Arenas.Add(newArena);

                await _dbContext.SaveChangesAsync();

                // -------------------------------------------------
                // SUCCESS MESSAGE
                // -------------------------------------------------

                await DisplayAlertAsync(
                    "Registration Successful",
                    "Your indoor arena has been registered successfully. You can now log in and complete the business setup.",
                    "OK");

                // -------------------------------------------------
                // CLEAR FORM
                // -------------------------------------------------

                TxtOwnerName.Text = string.Empty;
                TxtOwnerCNIC.Text = string.Empty;
                TxtArenaName.Text = string.Empty;
                TxtLocation.Text = string.Empty;
                TxtEmail.Text = string.Empty;
                TxtPassword.Text = string.Empty;
                TxtConfirmPassword.Text = string.Empty;
                TxtPhone.Text = string.Empty;
                TxtCoverImage.Text = string.Empty;
                TxtOwnerID.Text = string.Empty;

                // -------------------------------------------------
                // RETURN TO LOGIN
                //
                // IMPORTANT:
                // Do NOT use:
                //
                // Shell.Current.GoToAsync("///LoginPage")
                //
                // because RegisterArenaPage is not registered as a
                // Shell route in your current AppShell.
                // -------------------------------------------------

                if (Navigation.NavigationStack.Count > 1)
                {
                    await Navigation.PopAsync();
                }
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
                    "Registration Error",
                    $"Failed to register the arena:\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                _isRegistering = false;
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
                var mailAddress =
                    new System.Net.Mail.MailAddress(email);

                return mailAddress.Address.Equals(
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
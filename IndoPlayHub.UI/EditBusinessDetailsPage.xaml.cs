using IndoPlayHub.UI.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

namespace IndoPlayHub.UI
{
    public partial class EditBusinessDetailsPage : ContentPage
    {
        private readonly AppDbContext _dbContext;
        private readonly int _arenaId;

        private bool _isSaving;

        public EditBusinessDetailsPage(
            AppDbContext dbContext,
            int arenaId)
        {
            InitializeComponent();

            _dbContext = dbContext;
            _arenaId = arenaId;

            _ = LoadBusinessDetailsAsync();
        }

        // =========================================================
        // LOAD BUSINESS DETAILS
        // =========================================================

        private async Task LoadBusinessDetailsAsync()
        {
            try
            {
                if (_arenaId <= 0)
                {
                    await DisplayAlertAsync(
                        "Invalid Account",
                        "The logged-in arena account could not be identified.",
                        "OK");

                    return;
                }

                var arena = await _dbContext.Arenas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        a => a.ArenaID == _arenaId);

                if (arena == null)
                {
                    await DisplayAlertAsync(
                        "Account Not Found",
                        "The arena account could not be found.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // Populate form
                // -------------------------------------------------

                TxtArenaName.Text = arena.ArenaName;

                TxtEmail.Text = arena.Email;

                TxtPhone.Text = arena.PhoneNumber;

                TxtLocation.Text = arena.Location;

                TpOpening.Time = arena.OpeningTime;

                TpClosing.Time = arena.ClosingTime;

                // -------------------------------------------------
                // Account status
                // -------------------------------------------------

                LblAccountStatus.Text =
                    arena.IsSetupCompleted
                        ? "✓ Setup completed — Business is active"
                        : "⚠ Setup is incomplete";

                LblAccountStatus.TextColor =
                    arena.IsSetupCompleted
                        ? Color.FromArgb("#03DAC6")
                        : Color.FromArgb("#FFB74D");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Loading Error",
                    $"Unable to load business details:\n\n{ex.Message}",
                    "OK");
            }
        }

        // =========================================================
        // SAVE CHANGES
        // =========================================================

        private async void OnSaveChangesClicked(
            object? sender,
            EventArgs e)
        {
            if (_isSaving)
                return;

            _isSaving = true;

            try
            {
                // -------------------------------------------------
                // Validate Arena ID
                // -------------------------------------------------

                if (_arenaId <= 0)
                {
                    await DisplayAlertAsync(
                        "Invalid Account",
                        "Your arena account could not be identified. Please log in again.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // Validate Arena Name
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(TxtArenaName.Text))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter your arena/business name.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // Validate Email
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(TxtEmail.Text))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter your business email.",
                        "OK");

                    return;
                }

                string email = TxtEmail.Text.Trim();

                if (!IsValidEmail(email))
                {
                    await DisplayAlertAsync(
                        "Invalid Email",
                        "Please enter a valid business email address.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // Validate Phone
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(TxtPhone.Text))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter your contact number.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // Validate Location
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(TxtLocation.Text))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter your business location/address.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // Validate Operating Hours
                // -------------------------------------------------

                TimeSpan openingTime =
                    TpOpening.Time ?? TimeSpan.Zero;

                TimeSpan closingTime =
                    TpClosing.Time ?? TimeSpan.Zero;

                if (openingTime >= closingTime)
                {
                    await DisplayAlertAsync(
                        "Invalid Timings",
                        "Closing time must be later than opening time.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // Find current arena
                // -------------------------------------------------

                var arena = await _dbContext.Arenas
                    .FirstOrDefaultAsync(
                        a => a.ArenaID == _arenaId);

                if (arena == null)
                {
                    await DisplayAlertAsync(
                        "Account Not Found",
                        "The arena account could not be found. Please log in again.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // Check email conflict
                // -------------------------------------------------

                bool emailAlreadyUsed =
                    await _dbContext.Arenas.AnyAsync(
                        a =>
                            a.Email == email &&
                            a.ArenaID != _arenaId);

                if (emailAlreadyUsed)
                {
                    await DisplayAlertAsync(
                        "Email Already Exists",
                        "This email is already registered with another arena.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // Update arena
                // -------------------------------------------------

                arena.ArenaName =
                    TxtArenaName.Text.Trim();

                arena.Email =
                    email;

                arena.PhoneNumber =
                    TxtPhone.Text.Trim();

                arena.Location =
                    TxtLocation.Text.Trim();

                arena.OpeningTime =
                    openingTime;

                arena.ClosingTime =
                    closingTime;

                // -------------------------------------------------
                // Save changes
                // -------------------------------------------------

                await _dbContext.SaveChangesAsync();

                await DisplayAlertAsync(
                    "Changes Saved",
                    "Your business details have been successfully updated.",
                    "OK");

                // -------------------------------------------------
                // Return to Dashboard
                // -------------------------------------------------

                await Navigation.PopAsync();
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
                    "Save Error",
                    $"Unable to save your business details:\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                _isSaving = false;
            }
        }

        // =========================================================
        // CANCEL
        // =========================================================

        private async void OnCancelClicked(
            object? sender,
            EventArgs e)
        {
            await Navigation.PopAsync();
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
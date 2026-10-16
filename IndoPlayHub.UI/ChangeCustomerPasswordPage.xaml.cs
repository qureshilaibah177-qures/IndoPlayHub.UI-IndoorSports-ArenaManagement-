using IndoPlayHub.UI.Data;
using Microsoft.EntityFrameworkCore;

namespace IndoPlayHub.UI
{
    public partial class ChangeCustomerPasswordPage : ContentPage
    {
        private readonly AppDbContext _dbContext;
        private readonly int _customerId;

        public ChangeCustomerPasswordPage(
            AppDbContext dbContext,
            int customerId)
        {
            InitializeComponent();

            _dbContext = dbContext;
            _customerId = customerId;
        }

        // =========================================================
        // TOGGLE CURRENT PASSWORD
        // =========================================================

        private void OnToggleCurrentPasswordClicked(
            object? sender,
            EventArgs e)
        {
            TxtCurrentPassword.IsPassword =
                !TxtCurrentPassword.IsPassword;

            BtnCurrentPassword.Text =
                TxtCurrentPassword.IsPassword
                    ? "Show"
                    : "Hide";
        }

        // =========================================================
        // TOGGLE NEW PASSWORD
        // =========================================================

        private void OnToggleNewPasswordClicked(
            object? sender,
            EventArgs e)
        {
            TxtNewPassword.IsPassword =
                !TxtNewPassword.IsPassword;

            BtnNewPassword.Text =
                TxtNewPassword.IsPassword
                    ? "Show"
                    : "Hide";
        }

        // =========================================================
        // TOGGLE CONFIRM PASSWORD
        // =========================================================

        private void OnToggleConfirmPasswordClicked(
            object? sender,
            EventArgs e)
        {
            TxtConfirmPassword.IsPassword =
                !TxtConfirmPassword.IsPassword;

            BtnConfirmPassword.Text =
                TxtConfirmPassword.IsPassword
                    ? "Show"
                    : "Hide";
        }

        // =========================================================
        // CHANGE PASSWORD
        // =========================================================

        private async void OnChangePasswordClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                string currentPassword =
                    TxtCurrentPassword.Text?.Trim() ?? string.Empty;

                string newPassword =
                    TxtNewPassword.Text?.Trim() ?? string.Empty;

                string confirmPassword =
                    TxtConfirmPassword.Text?.Trim() ?? string.Empty;

                // -------------------------------------------------
                // VALIDATION
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(currentPassword))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter your current password.",
                        "OK");

                    return;
                }

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter a new password.",
                        "OK");

                    return;
                }

                if (newPassword.Length < 6)
                {
                    await DisplayAlertAsync(
                        "Invalid Password",
                        "New password must contain at least 6 characters.",
                        "OK");

                    return;
                }

                if (newPassword != confirmPassword)
                {
                    await DisplayAlertAsync(
                        "Password Mismatch",
                        "New password and confirmation password do not match.",
                        "OK");

                    return;
                }

                if (currentPassword == newPassword)
                {
                    await DisplayAlertAsync(
                        "Invalid Password",
                        "Your new password must be different from your current password.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // FIND CUSTOMER
                // -------------------------------------------------

                var customer = await _dbContext.Customers
                    .FirstOrDefaultAsync(c =>
                        c.CustomerID == _customerId &&
                        c.IsActive);

                if (customer == null)
                {
                    await DisplayAlertAsync(
                        "Account Not Found",
                        "Your customer account could not be found.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // VERIFY CURRENT PASSWORD
                // -------------------------------------------------

                if (customer.Password != currentPassword)
                {
                    await DisplayAlertAsync(
                        "Incorrect Password",
                        "The current password you entered is incorrect.",
                        "OK");

                    return;
                }

                // -------------------------------------------------
                // UPDATE PASSWORD
                // -------------------------------------------------

                customer.Password = newPassword;

                await _dbContext.SaveChangesAsync();

                // -------------------------------------------------
                // CLEAR FORM
                // -------------------------------------------------

                TxtCurrentPassword.Text = string.Empty;
                TxtNewPassword.Text = string.Empty;
                TxtConfirmPassword.Text = string.Empty;

                await DisplayAlertAsync(
                    "Password Changed",
                    "Your password has been changed successfully.",
                    "OK");

                await Navigation.PopAsync();
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
    }
}
using IndoPlayHub.UI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage;

namespace IndoPlayHub.UI
{
    public partial class EditCustomerProfilePage : ContentPage
    {
        private readonly AppDbContext _dbContext;
        private readonly int _customerId;


    public EditCustomerProfilePage(
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
                var customer = await _dbContext.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c =>
                        c.CustomerID == _customerId);

                if (customer == null)
                {
                    await DisplayAlertAsync(
                        "Account Not Found",
                        "Your customer account could not be found.",
                        "OK");

                    await Navigation.PopAsync();

                    return;
                }

                TxtFullName.Text = customer.FullName;

                TxtEmail.Text = customer.Email;

                TxtPhone.Text = customer.PhoneNumber;

                TxtProfileImage.Text =
                    customer.ProfileImageUrl ?? string.Empty;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"Failed to load your profile.\n\n{ex.Message}",
                    "OK");
            }
        }

        // =========================================================
        // BROWSE PROFILE IMAGE
        // =========================================================

        private async void OnBrowseProfileImageClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                var options = new PickOptions
                {
                    PickerTitle = "Select Profile Picture",
                    FileTypes = FilePickerFileType.Images
                };

                FileResult? result =
                    await FilePicker.Default.PickAsync(options);

                if (result == null)
                    return;

                // -------------------------------------------------
                // STORE THE SELECTED LOCAL FILE PATH
                // -------------------------------------------------

                TxtProfileImage.Text = result.FullPath;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Unable to Select Image",
                    $"The profile picture could not be selected.\n\n{ex.Message}",
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
            try
            {
                string fullName =
                    TxtFullName.Text?.Trim() ?? string.Empty;

                string email =
                    TxtEmail.Text?.Trim() ?? string.Empty;

                string phone =
                    TxtPhone.Text?.Trim() ?? string.Empty;

                string profileImage =
                    TxtProfileImage.Text?.Trim() ?? string.Empty;

                // =================================================
                // NAME
                // =================================================

                if (string.IsNullOrWhiteSpace(fullName))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter your full name.",
                        "OK");

                    return;
                }

                if (fullName.Length > 100)
                {
                    await DisplayAlertAsync(
                        "Invalid Name",
                        "Full name cannot exceed 100 characters.",
                        "OK");

                    return;
                }

                // =================================================
                // EMAIL
                // =================================================

                if (string.IsNullOrWhiteSpace(email))
                {
                    await DisplayAlertAsync(
                        "Invalid Email",
                        "Please enter a valid email address.",
                        "OK");

                    return;
                }

                try
                {
                    var mailAddress =
                        new System.Net.Mail.MailAddress(email);

                    if (!mailAddress.Address.Equals(
                            email,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        await DisplayAlertAsync(
                            "Invalid Email",
                            "Please enter a valid email address.",
                            "OK");

                        return;
                    }
                }
                catch (FormatException)
                {
                    await DisplayAlertAsync(
                        "Invalid Email",
                        "Please enter a valid email address.",
                        "OK");

                    return;
                }

                if (email.Length > 100)
                {
                    await DisplayAlertAsync(
                        "Invalid Email",
                        "Email cannot exceed 100 characters.",
                        "OK");

                    return;
                }

                // =================================================
                // PHONE
                // =================================================

                if (string.IsNullOrWhiteSpace(phone))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter your phone number.",
                        "OK");

                    return;
                }

                if (phone.Length > 20)
                {
                    await DisplayAlertAsync(
                        "Invalid Phone",
                        "Phone number cannot exceed 20 characters.",
                        "OK");

                    return;
                }

                // =================================================
                // PROFILE IMAGE
                // =================================================

                if (!string.IsNullOrWhiteSpace(profileImage))
                {
                    bool localFileExists =
                        File.Exists(profileImage);

                    if (!localFileExists)
                    {
                        if (profileImage.Length > 255)
                        {
                            await DisplayAlertAsync(
                                "Invalid Image",
                                "Image URL cannot exceed 255 characters.",
                                "OK");

                            return;
                        }

                        if (!Uri.TryCreate(
                                profileImage,
                                UriKind.Absolute,
                                out Uri? imageUri))
                        {
                            await DisplayAlertAsync(
                                "Invalid Image",
                                "Please enter a valid HTTP/HTTPS image URL or choose an image from your device.",
                                "OK");

                            return;
                        }

                        if (imageUri.Scheme != Uri.UriSchemeHttp &&
                            imageUri.Scheme != Uri.UriSchemeHttps)
                        {
                            await DisplayAlertAsync(
                                "Invalid Image",
                                "Please enter a valid HTTP/HTTPS image URL or choose an image from your device.",
                                "OK");

                            return;
                        }
                    }
                }

                // =================================================
                // FIND CUSTOMER
                // =================================================

                var customer = await _dbContext.Customers
                    .FirstOrDefaultAsync(c =>
                        c.CustomerID == _customerId);

                if (customer == null)
                {
                    await DisplayAlertAsync(
                        "Account Not Found",
                        "Your customer account could not be found.",
                        "OK");

                    return;
                }

                // =================================================
                // DUPLICATE EMAIL
                // =================================================

                bool emailExists =
                    await _dbContext.Customers.AnyAsync(c =>
                        c.CustomerID != _customerId &&
                        c.Email == email);

                if (emailExists)
                {
                    await DisplayAlertAsync(
                        "Email Already Used",
                        "Another customer account already uses this email.",
                        "OK");

                    return;
                }

                // =================================================
                // DUPLICATE PHONE
                // =================================================

                bool phoneExists =
                    await _dbContext.Customers.AnyAsync(c =>
                        c.CustomerID != _customerId &&
                        c.PhoneNumber == phone);

                if (phoneExists)
                {
                    await DisplayAlertAsync(
                        "Phone Already Used",
                        "Another customer account already uses this phone number.",
                        "OK");

                    return;
                }

                // =================================================
                // UPDATE CUSTOMER
                // =================================================

                customer.FullName = fullName;

                customer.Email = email;

                customer.PhoneNumber = phone;

                customer.ProfileImageUrl =
                    string.IsNullOrWhiteSpace(profileImage)
                        ? null
                        : profileImage;

                await _dbContext.SaveChangesAsync();

                // =================================================
                // UPDATE SESSION EMAIL
                // =================================================

                Preferences.Set(
                    "CustomerEmail",
                    customer.Email);

                // =================================================
                // SUCCESS
                // =================================================

                await DisplayAlertAsync(
                    "Profile Updated",
                    "Your profile has been updated successfully.",
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

        // =========================================================
        // CANCEL
        // =========================================================

        private async void OnCancelClicked(
            object? sender,
            EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }


}

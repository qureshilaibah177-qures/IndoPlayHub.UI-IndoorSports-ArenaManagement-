using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

namespace IndoPlayHub.UI
{
    public partial class CustomerRegisterPage : ContentPage
    {
        private readonly AppDbContext _dbContext;

        private bool _isRegistering;
        private bool _passwordVisible;

        public CustomerRegisterPage(AppDbContext dbContext)
        {
            InitializeComponent();

            _dbContext = dbContext;
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
        // REGISTER
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
                // READ FORM
                // -------------------------------------------------

                string fullName =
                    TxtFullName.Text?.Trim() ?? string.Empty;

                string email =
                    TxtEmail.Text?.Trim().ToLowerInvariant()
                    ?? string.Empty;

                string phone =
                    TxtPhone.Text?.Trim() ?? string.Empty;

                string password =
                    TxtPassword.Text ?? string.Empty;

                string confirmPassword =
                    TxtConfirmPassword.Text ?? string.Empty;

                string profileImage =
                    TxtProfileImage.Text?.Trim() ?? string.Empty;


                // -------------------------------------------------
                // FULL NAME
                // -------------------------------------------------

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


                // -------------------------------------------------
                // EMAIL
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(email))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please enter your email address.",
                        "OK");

                    return;
                }

                try
                {
                    var mail = new MailAddress(email);

                    if (mail.Address != email)
                    {
                        throw new FormatException();
                    }
                }
                catch
                {
                    await DisplayAlertAsync(
                        "Invalid Email",
                        "Please enter a valid email address.",
                        "OK");

                    return;
                }


                // -------------------------------------------------
                // PHONE
                // -------------------------------------------------

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


                // -------------------------------------------------
                // PASSWORD
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(password))
                {
                    await DisplayAlertAsync(
                        "Required",
                        "Please create a password.",
                        "OK");

                    return;
                }

                if (password.Length < 6)
                {
                    await DisplayAlertAsync(
                        "Weak Password",
                        "Password must contain at least 6 characters.",
                        "OK");

                    return;
                }

                if (password.Length > 255)
                {
                    await DisplayAlertAsync(
                        "Invalid Password",
                        "Password cannot exceed 255 characters.",
                        "OK");

                    return;
                }


                // -------------------------------------------------
                // CONFIRM PASSWORD
                // -------------------------------------------------

                if (password != confirmPassword)
                {
                    await DisplayAlertAsync(
                        "Password Mismatch",
                        "Password and confirm password do not match.",
                        "OK");

                    return;
                }


                // -------------------------------------------------
                // TERMS
                // -------------------------------------------------

                if (!ChkTerms.IsChecked)
                {
                    await DisplayAlertAsync(
                        "Terms Required",
                        "Please agree to the Terms & Conditions before creating your account.",
                        "OK");

                    return;
                }


                // -------------------------------------------------
                // PROFILE IMAGE
                // -------------------------------------------------

                if (!string.IsNullOrWhiteSpace(profileImage))
                {
                    if (profileImage.Length > 255)
                    {
                        await DisplayAlertAsync(
                            "Invalid Image URL",
                            "Profile image URL cannot exceed 255 characters.",
                            "OK");

                        return;
                    }

                    if (!Uri.TryCreate(
                            profileImage,
                            UriKind.Absolute,
                            out Uri? imageUri) ||
                        (imageUri.Scheme != Uri.UriSchemeHttp &&
                         imageUri.Scheme != Uri.UriSchemeHttps))
                    {
                        await DisplayAlertAsync(
                            "Invalid Image URL",
                            "Please enter a valid HTTP or HTTPS image URL.",
                            "OK");

                        return;
                    }
                }
                else
                {
                    profileImage = string.Empty;
                }


                // -------------------------------------------------
                // DUPLICATE EMAIL
                // -------------------------------------------------

                bool emailExists =
                    await _dbContext.Customers
                        .AsNoTracking()
                        .AnyAsync(c => c.Email == email);

                if (emailExists)
                {
                    await DisplayAlertAsync(
                        "Email Already Registered",
                        "An account with this email already exists.",
                        "OK");

                    return;
                }


                // -------------------------------------------------
                // DUPLICATE PHONE
                // -------------------------------------------------

                bool phoneExists =
                    await _dbContext.Customers
                        .AsNoTracking()
                        .AnyAsync(c => c.PhoneNumber == phone);

                if (phoneExists)
                {
                    await DisplayAlertAsync(
                        "Phone Already Registered",
                        "An account with this phone number already exists.",
                        "OK");

                    return;
                }


                // -------------------------------------------------
                // CREATE CUSTOMER
                // -------------------------------------------------

                var customer = new Customer
                {
                    FullName = fullName,
                    Email = email,
                    PhoneNumber = phone,
                    Password = password,
                    ProfileImageUrl =
                        string.IsNullOrWhiteSpace(profileImage)
                            ? null
                            : profileImage,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _dbContext.Customers.Add(customer);

                await _dbContext.SaveChangesAsync();


                // -------------------------------------------------
                // SUCCESS
                // -------------------------------------------------

                await DisplayAlertAsync(
                    "Account Created",
                    $"Welcome to IndoPlayHub, {fullName}!",
                    "Continue");


                // Clear form

                TxtFullName.Text = string.Empty;
                TxtEmail.Text = string.Empty;
                TxtPhone.Text = string.Empty;
                TxtPassword.Text = string.Empty;
                TxtConfirmPassword.Text = string.Empty;
                TxtProfileImage.Text = string.Empty;

                ChkTerms.IsChecked = false;

                _passwordVisible = false;
                TxtPassword.IsPassword = true;
                BtnPasswordVisibility.Text = "👁";


                // -------------------------------------------------
                // GO TO LOGIN
                // -------------------------------------------------

                await Shell.Current.GoToAsync(
                    "CustomerLoginPage");
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
            finally
            {
                _isRegistering = false;
            }
        }


        // =========================================================
        // LOGIN LINK
        // =========================================================

        private async void OnLoginTapped(
            object? sender,
            TappedEventArgs e)
        {
            await Shell.Current.GoToAsync(
                "CustomerLoginPage");
        }
    }
}
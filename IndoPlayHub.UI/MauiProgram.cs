using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IndoPlayHub.UI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            // =========================================================
            // MAUI APPLICATION
            // =========================================================

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont(
                        "OpenSans-Regular.ttf",
                        "OpenSansRegular");

                    fonts.AddFont(
                        "OpenSans-Semibold.ttf",
                        "OpenSansSemibold");
                });


            // =========================================================
            // DATABASE
            // =========================================================

            const string connectionString =
                "Server=LAPTOP-2J7MQTGO;" +
                "Database=IndoPlayHubDB;" +
                "Trusted_Connection=True;" +
                "TrustServerCertificate=True;";

            builder.Services.AddDbContext<AppDbContext>(
                options =>
                {
                    options.UseSqlServer(connectionString);
                },
                ServiceLifetime.Transient);


            // =========================================================
            // SERVICES
            // =========================================================

            builder.Services.AddTransient<CustomerSessionService>();
            builder.Services.AddTransient<NotificationService>();


            // =========================================================
            // APPLICATION ROOT
            // =========================================================

            builder.Services.AddTransient<AppShell>();


            // =========================================================
            // MAIN / PORTAL
            // =========================================================

            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<PortalSelectionPage>();


            // =========================================================
            // OWNER / ADMIN AUTHENTICATION
            // =========================================================

            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<RegisterArenaPage>();


            // =========================================================
            // CUSTOMER AUTHENTICATION
            // =========================================================

            builder.Services.AddTransient<CustomerRegisterPage>();
            builder.Services.AddTransient<CustomerLoginPage>();


            // =========================================================
            // CUSTOMER HOME
            // =========================================================

            builder.Services.AddTransient<CustomerHomePage>();
            builder.Services.AddTransient<CustomerNotificationsPage>();


            // =========================================================
            // CUSTOMER LOCATION / ARENA DISCOVERY
            // =========================================================

            builder.Services.AddTransient<CustomerLocationPage>();
            builder.Services.AddTransient<CustomerSportArenasPage>();
            builder.Services.AddTransient<CustomerArenaDetailsPage>();


            // =========================================================
            // CUSTOMER ARENA / COURT / MENU
            // =========================================================

            builder.Services.AddTransient<CustomerMenuPage>();
            builder.Services.AddTransient<CustomerCourtSelectionPage>();
            builder.Services.AddTransient<CustomerDateTimeSelectionPage>();


            // =========================================================
            // CUSTOMER BOOKING FLOW
            // =========================================================

            builder.Services.AddTransient<CustomerBookingSummaryPage>();
            builder.Services.AddTransient<CustomerBookingConfirmationPage>();


            // =========================================================
            // CUSTOMER BOOKINGS
            // =========================================================

            builder.Services.AddTransient<CustomerMyBookingsPage>();
            builder.Services.AddTransient<CustomerBookingDetailsPage>();


            // =========================================================
            // CUSTOMER ACCOUNT
            // =========================================================

            builder.Services.AddTransient<CustomerProfilePage>();
            builder.Services.AddTransient<EditCustomerProfilePage>();
            builder.Services.AddTransient<ChangeCustomerPasswordPage>();


            // =========================================================
            // CUSTOMER SUPPORT
            // =========================================================

            builder.Services.AddTransient<CustomerSupportPage>();


            // =========================================================
            // ARENA SETUP
            // =========================================================

            builder.Services.AddTransient<SetupArenaPage>();


            // =========================================================
            // ADMIN / OWNER DASHBOARD
            // =========================================================

            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<CourtsPage>();
            builder.Services.AddTransient<MenuPage>();


            // =========================================================
            // SPORT MANAGEMENT
            // =========================================================

            builder.Services.AddTransient<ManageSportsPage>();
            builder.Services.AddTransient<EditSportPage>();
            builder.Services.AddTransient<AddSportPage>();


            // =========================================================
            // DEBUG LOGGING
            // =========================================================

#if DEBUG
            builder.Logging.AddDebug();
#endif


            // =========================================================
            // BUILD
            // =========================================================

            return builder.Build();
        }
    }
}
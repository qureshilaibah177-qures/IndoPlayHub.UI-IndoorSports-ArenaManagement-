using IndoPlayHub.UI.Models;
using IndoPlayHub.UI.Services;

namespace IndoPlayHub.UI
{
    public partial class AppShell : Shell
    {
        // =========================================================
        // SERVICES
        // =========================================================

        private readonly CustomerSessionService _sessionService;


        // =========================================================
        // STATE
        // =========================================================

        private bool _sessionChecked;
        private bool _isRestoringSession;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public AppShell(
            CustomerSessionService sessionService)
        {
            InitializeComponent();

            _sessionService =
                sessionService
                ?? throw new ArgumentNullException(
                    nameof(sessionService));

            RegisterRoutes();
        }


        // =========================================================
        // ROUTE REGISTRATION
        //
        // PortalSelectionPage is the Shell root and therefore
        // does not need to be registered here.
        // =========================================================

        private void RegisterRoutes()
        {
            // =====================================================
            // OWNER / ADMIN AUTHENTICATION
            // =====================================================

            RegisterRoute<LoginPage>("LoginPage");
            RegisterRoute<RegisterArenaPage>("RegisterArenaPage");


            // =====================================================
            // CUSTOMER AUTHENTICATION
            // =====================================================

            RegisterRoute<CustomerRegisterPage>(
                "CustomerRegisterPage");

            RegisterRoute<CustomerLoginPage>(
                "CustomerLoginPage");


            // =====================================================
            // CUSTOMER HOME
            // =====================================================

            RegisterRoute<CustomerHomePage>(
                "CustomerHomePage");


            // =====================================================
            // CUSTOMER NOTIFICATIONS
            // =====================================================

            RegisterRoute<CustomerNotificationsPage>(
                "CustomerNotificationsPage");


            // =====================================================
            // CUSTOMER LOCATION / DISCOVERY
            // =====================================================

            RegisterRoute<CustomerLocationPage>(
                "CustomerLocationPage");

            RegisterRoute<CustomerSportArenasPage>(
                "CustomerSportArenasPage");

            RegisterRoute<CustomerArenaDetailsPage>(
                "CustomerArenaDetailsPage");


            // =====================================================
            // CUSTOMER ARENA / MENU / COURT SELECTION
            // =====================================================

            RegisterRoute<CustomerMenuPage>(
                "CustomerMenuPage");

            RegisterRoute<CustomerCourtSelectionPage>(
                "CustomerCourtSelectionPage");

            RegisterRoute<CustomerDateTimeSelectionPage>(
                "CustomerDateTimeSelectionPage");


            // =====================================================
            // CUSTOMER BOOKING FLOW
            // =====================================================

            RegisterRoute<CustomerBookingSummaryPage>(
                "CustomerBookingSummaryPage");

            RegisterRoute<CustomerBookingConfirmationPage>(
                "CustomerBookingConfirmationPage");


            // =====================================================
            // CUSTOMER BOOKINGS
            // =====================================================

            RegisterRoute<CustomerMyBookingsPage>(
                "CustomerMyBookingsPage");

            RegisterRoute<CustomerBookingDetailsPage>(
                "CustomerBookingDetailsPage");


            // =====================================================
            // CUSTOMER ACCOUNT
            // =====================================================

            RegisterRoute<CustomerProfilePage>(
                "CustomerProfilePage");

            RegisterRoute<EditCustomerProfilePage>(
                "EditCustomerProfilePage");

            RegisterRoute<ChangeCustomerPasswordPage>(
                "ChangeCustomerPasswordPage");


            // =====================================================
            // CUSTOMER SUPPORT
            // =====================================================

            RegisterRoute<CustomerSupportPage>(
                "CustomerSupportPage");
        }


        // =========================================================
        // SAFE ROUTE REGISTRATION
        // =========================================================

        private static void RegisterRoute<TPage>(
            string route)
            where TPage : Page
        {
            if (string.IsNullOrWhiteSpace(route))
            {
                throw new ArgumentException(
                    "Route name cannot be empty.",
                    nameof(route));
            }

            Routing.RegisterRoute(
                route,
                typeof(TPage));
        }


        // =========================================================
        // SHELL APPEARING
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // -----------------------------------------------------
            // PREVENT DUPLICATE SESSION RESTORATION
            // -----------------------------------------------------

            if (_sessionChecked ||
                _isRestoringSession)
            {
                return;
            }

            _sessionChecked = true;

            await RestoreCustomerSessionAsync();
        }


        // =========================================================
        // RESTORE CUSTOMER SESSION
        // =========================================================

        private async Task RestoreCustomerSessionAsync()
        {
            if (_isRestoringSession)
                return;

            _isRestoringSession = true;

            try
            {
                // -------------------------------------------------
                // RESTORE CUSTOMER SESSION
                // -------------------------------------------------

                Customer? customer =
                    await _sessionService
                        .RestoreSessionAsync();


                // -------------------------------------------------
                // NO ACTIVE CUSTOMER SESSION
                //
                // Stay on PortalSelectionPage.
                // -------------------------------------------------

                if (customer == null ||
                    customer.CustomerID <= 0)
                {
                    return;
                }


                // -------------------------------------------------
                // ACTIVE CUSTOMER SESSION
                //
                // Open customer home.
                // -------------------------------------------------

                await MainThread.InvokeOnMainThreadAsync(
                    async () =>
                    {
                        try
                        {
                            await GoToAsync(
                                "CustomerHomePage");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                "Customer home navigation failed: " +
                                ex);
                        }
                    });
            }
            catch (Exception ex)
            {
                // -------------------------------------------------
                // SESSION RESTORATION MUST NEVER CRASH THE APP
                // -------------------------------------------------

                System.Diagnostics.Debug.WriteLine(
                    "Customer session restoration failed: " +
                    ex);
            }
            finally
            {
                _isRestoringSession = false;
            }
        }
    }
}
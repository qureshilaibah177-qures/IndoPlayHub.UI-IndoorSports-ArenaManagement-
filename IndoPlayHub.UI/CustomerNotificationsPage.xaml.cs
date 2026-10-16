using IndoPlayHub.UI.Services;

namespace IndoPlayHub.UI
{
    public partial class CustomerNotificationsPage : ContentPage
    {
        // =========================================================
        // SERVICES
        // =========================================================

        private readonly NotificationService _notificationService;
        private readonly CustomerSessionService _customerSessionService;


        // =========================================================
        // CUSTOMER
        // =========================================================

        private int _customerID;


        // =========================================================
        // STATE
        // =========================================================

        private bool _isLoading;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public CustomerNotificationsPage(
            NotificationService notificationService,
            CustomerSessionService customerSessionService)
        {
            InitializeComponent();

            _notificationService = notificationService;
            _customerSessionService = customerSessionService;
        }


        // =========================================================
        // PAGE APPEARING
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_isLoading)
                return;

            await LoadCustomerNotificationsAsync();
        }


        // =========================================================
        // LOAD CUSTOMER + NOTIFICATIONS
        // =========================================================

        private async Task LoadCustomerNotificationsAsync()
        {
            if (_isLoading)
                return;

            _isLoading = true;

            try
            {
                // -------------------------------------------------
                // LOADING STATE
                // -------------------------------------------------

                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                EmptyState.IsVisible = false;
                NotificationsCollection.IsVisible = false;

                BtnMarkAllRead.IsEnabled = false;


                // -------------------------------------------------
                // GET LOGGED-IN CUSTOMER
                // -------------------------------------------------

                int? customerId =
                    await _customerSessionService
                        .GetCustomerIdAsync();


                // -------------------------------------------------
                // SESSION NOT FOUND
                // -------------------------------------------------

                if (!customerId.HasValue ||
                    customerId.Value <= 0)
                {
                    _customerID = 0;

                    NotificationsCollection.ItemsSource = null;

                    EmptyState.IsVisible = true;
                    NotificationsCollection.IsVisible = false;

                    LblNotificationSummary.Text =
                        "Please log in to view your notifications.";

                    return;
                }


                _customerID = customerId.Value;


                // -------------------------------------------------
                // LOAD NOTIFICATIONS
                // -------------------------------------------------

                var notifications =
                    await _notificationService
                        .GetCustomerNotificationsAsync(
                            _customerID);


                NotificationsCollection.ItemsSource =
                    notifications;


                // -------------------------------------------------
                // GET UNREAD COUNT
                // -------------------------------------------------

                int unreadCount =
                    await _notificationService
                        .GetUnreadCountAsync(
                            _customerID);


                // -------------------------------------------------
                // EMPTY STATE
                // -------------------------------------------------

                if (notifications == null ||
                    notifications.Count == 0)
                {
                    EmptyState.IsVisible = true;
                    NotificationsCollection.IsVisible = false;

                    LblNotificationSummary.Text =
                        "You have no notifications.";

                    BtnMarkAllRead.IsEnabled = false;

                    return;
                }


                // -------------------------------------------------
                // DISPLAY LIST
                // -------------------------------------------------

                EmptyState.IsVisible = false;
                NotificationsCollection.IsVisible = true;


                // -------------------------------------------------
                // SUMMARY
                // -------------------------------------------------

                if (unreadCount > 0)
                {
                    LblNotificationSummary.Text =
                        $"{unreadCount} unread notification" +
                        (unreadCount == 1 ? "" : "s");
                }
                else
                {
                    LblNotificationSummary.Text =
                        "You're all caught up.";
                }


                // -------------------------------------------------
                // MARK ALL BUTTON
                // -------------------------------------------------

                BtnMarkAllRead.IsEnabled =
                    unreadCount > 0;
            }
            catch (Exception ex)
            {
                NotificationsCollection.ItemsSource = null;

                EmptyState.IsVisible = true;
                NotificationsCollection.IsVisible = false;

                BtnMarkAllRead.IsEnabled = false;

                LblNotificationSummary.Text =
                    "Unable to load notifications.";

                await DisplayAlertAsync(
                    "Notifications",
                    "Unable to load notifications.\n\n" +
                    ex.Message,
                    "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;

                _isLoading = false;
            }
        }


        // =========================================================
        // MARK ONE NOTIFICATION AS READ
        // =========================================================

        private async void OnMarkAsReadClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                // -------------------------------------------------
                // VALIDATE CUSTOMER SESSION
                // -------------------------------------------------

                if (_customerID <= 0)
                {
                    int? customerId =
                        await _customerSessionService
                            .GetCustomerIdAsync();

                    if (!customerId.HasValue ||
                        customerId.Value <= 0)
                    {
                        await DisplayAlertAsync(
                            "Login Required",
                            "Please login to continue.",
                            "OK");

                        await Shell.Current.GoToAsync(
                            "CustomerLoginPage");

                        return;
                    }

                    _customerID = customerId.Value;
                }


                // -------------------------------------------------
                // VALIDATE BUTTON
                // -------------------------------------------------

                if (sender is not Button button)
                    return;


                // -------------------------------------------------
                // GET NOTIFICATION ID
                // -------------------------------------------------

                if (button.CommandParameter is not int notificationID)
                    return;


                if (notificationID <= 0)
                    return;


                // -------------------------------------------------
                // MARK AS READ
                // IMPORTANT:
                // CUSTOMER ID + NOTIFICATION ID
                // -------------------------------------------------

                bool success =
                    await _notificationService
                        .MarkAsReadAsync(
                            _customerID,
                            notificationID);


                if (!success)
                {
                    await DisplayAlertAsync(
                        "Notifications",
                        "The notification could not be updated.",
                        "OK");

                    return;
                }


                // -------------------------------------------------
                // REFRESH
                // -------------------------------------------------

                await LoadCustomerNotificationsAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Notifications",
                    "Unable to mark the notification as read.\n\n" +
                    ex.Message,
                    "OK");
            }
        }


        // =========================================================
        // MARK ALL NOTIFICATIONS AS READ
        // =========================================================

        private async void OnMarkAllReadClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                // -------------------------------------------------
                // VALIDATE CUSTOMER
                // -------------------------------------------------

                if (_customerID <= 0)
                {
                    int? customerId =
                        await _customerSessionService
                            .GetCustomerIdAsync();

                    if (!customerId.HasValue ||
                        customerId.Value <= 0)
                    {
                        await DisplayAlertAsync(
                            "Login Required",
                            "Please login to continue.",
                            "OK");

                        await Shell.Current.GoToAsync(
                            "CustomerLoginPage");

                        return;
                    }

                    _customerID = customerId.Value;
                }


                // -------------------------------------------------
                // DISABLE IMMEDIATELY
                // -------------------------------------------------

                BtnMarkAllRead.IsEnabled = false;


                // -------------------------------------------------
                // MARK ALL
                // -------------------------------------------------

                await _notificationService
                    .MarkAllAsReadAsync(
                        _customerID);


                // -------------------------------------------------
                // REFRESH
                // -------------------------------------------------

                await LoadCustomerNotificationsAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Notifications",
                    "Unable to mark notifications as read.\n\n" +
                    ex.Message,
                    "OK");

                await LoadCustomerNotificationsAsync();
            }
        }
    }
}
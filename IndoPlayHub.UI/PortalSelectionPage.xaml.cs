namespace IndoPlayHub.UI
{
    public partial class PortalSelectionPage : ContentPage
    {
        public PortalSelectionPage()
        {
            InitializeComponent();
        }

        // =========================================================
        // ARENA / ADMIN PORTAL
        // =========================================================

        private async void OnAdminPortalClicked(
            object? sender,
            EventArgs e)
        {
            await Shell.Current.GoToAsync("LoginPage");
        }


        // =========================================================
        // CUSTOMER PORTAL
        // =========================================================

        private async void OnCustomerPortalClicked(
            object? sender,
            EventArgs e)
        {
            await Shell.Current.GoToAsync(
                "CustomerLoginPage");
        }
    }
}
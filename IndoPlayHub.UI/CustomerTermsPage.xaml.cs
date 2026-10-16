namespace IndoPlayHub.UI
{
    public partial class CustomerTermsPage : ContentPage
    {
        public CustomerTermsPage()
        {
            InitializeComponent();
        }


    private async void OnBackClicked(
        object? sender,
        EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }


}

namespace IndoPlayHub.UI
{
    public partial class CustomerPrivacyPage : ContentPage
    {
        public CustomerPrivacyPage()
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

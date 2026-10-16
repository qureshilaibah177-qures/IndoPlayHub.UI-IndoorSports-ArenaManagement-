using IndoPlayHub.UI.Data;
using Microsoft.EntityFrameworkCore;

namespace IndoPlayHub.UI
{
    public partial class ArenaHubPage : ContentPage
    {
        private readonly AppDbContext _dbContext;
        private readonly int _loggedInUserId;

        public ArenaHubPage(AppDbContext dbContext, int loggedInUserId = 1)
        {
            InitializeComponent();
            _dbContext = dbContext;
            _loggedInUserId = loggedInUserId;
        }

        private async void OnSetupNewArenaClicked(object sender, EventArgs e)
        {
            // Yeh aapke naye arena setup wizard (Next -> Next -> Next flow) par le jayega
            await Navigation.PushAsync(new SetupArenaPage(_dbContext, _loggedInUserId));
        }

        private async void OnOpenMultanDashboardClicked(object sender, EventArgs e)
        {
            try
            {
                // Database se Multan Arena find karna ya default ID 1 dena
                var multanArena = await _dbContext.Arenas
                    .FirstOrDefaultAsync(a => a.ArenaName.Contains("Multan") || a.Location.Contains("Multan"));

                int targetArenaId = multanArena != null ? multanArena.ArenaID : 1;

                // Seedha Multan Dashboard kholna jahan Courts aur Menu buttons independently work karenge
                await Navigation.PushAsync(new DashboardPage(_dbContext, targetArenaId));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not load Multan Arena dashboard: {ex.Message}", "OK");
            }
        }
    }
}
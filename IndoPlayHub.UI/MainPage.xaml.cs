using IndoPlayHub.UI.Data;
using Microsoft.EntityFrameworkCore;

namespace IndoPlayHub.UI
{
    public partial class MainPage : ContentPage
    {
        private readonly AppDbContext _dbContext;

        // Dependency Injection ke zariye DbContext ko yahan laye hain
        public MainPage(AppDbContext dbContext)
        {
            InitializeComponent();
            _dbContext = dbContext;
        }

        private async void OnCounterClicked(object? sender, EventArgs e)
        {
            try
            {
                // Database se Arenas ki taadad (count) nikal rahe hain
                int arenaCount = await _dbContext.Arenas.CountAsync();

                // Pehli Arena ka naam nikal rahe hain test ke liye
                var firstArena = await _dbContext.Arenas.FirstOrDefaultAsync();
                string arenaName = firstArena != null ? firstArena.ArenaName : "No Arena Found";

                CounterBtn.Text = "Connected! Arenas: " + arenaCount;

                // Debug output mein bhi print karwa dein ge
                System.Diagnostics.Debug.WriteLine($"---> Database Connected Successfully! Total Arenas: {arenaCount}, First Arena: {arenaName}");
            }
            catch (Exception ex)
            {
                CounterBtn.Text = "Connection Failed!";
                System.Diagnostics.Debug.WriteLine($"---> Error: {ex.Message}");
            }
        }
    }
}
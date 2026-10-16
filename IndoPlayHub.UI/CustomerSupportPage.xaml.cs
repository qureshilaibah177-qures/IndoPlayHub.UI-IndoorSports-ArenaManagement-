using Microsoft.Maui.Controls.Shapes;

namespace IndoPlayHub.UI
{
    public partial class CustomerSupportPage : ContentPage
    {
// =========================================================
// FAQ MODEL
// =========================================================

    private sealed class FaqItem
        {
            public string Question { get; init; } = string.Empty;

            public string Answer { get; init; } = string.Empty;
        }

        // =========================================================
        // FAQ DATA
        // =========================================================

        private readonly List<FaqItem> _allFaqs =
            new()
            {
            new FaqItem
            {
                Question = "How do I book a court?",
                Answer =
                    "Choose a sport from Explore Sports, select an arena, " +
                    "open the arena details, choose an available court, " +
                    "select your date and time, review the Booking Summary, " +
                    "and confirm your booking."
            },

            new FaqItem
            {
                Question = "How do I cancel my booking?",
                Answer =
                    "Open My Bookings and select the booking you want to cancel. " +
                    "If cancellation is available, follow the cancellation " +
                    "instructions shown for that booking."
            },

            new FaqItem
            {
                Question = "How do payments work?",
                Answer =
                    "Available payment information is shown during the booking " +
                    "process. Review the payment status and booking summary " +
                    "before confirming your booking."
            },

            new FaqItem
            {
                Question = "What happens if an arena is closed?",
                Answer =
                    "If an arena becomes unavailable, your booking may need to " +
                    "be cancelled or rescheduled. Check your booking details " +
                    "and contact support if you need assistance."
            },

            new FaqItem
            {
                Question = "How do refunds work?",
                Answer =
                    "Refund eligibility depends on the booking and cancellation " +
                    "conditions. If you believe a refund applies, contact support " +
                    "and provide your booking details."
            },

            new FaqItem
            {
                Question = "How can I update my profile?",
                Answer =
                    "Open My Profile, select Edit Profile, update your information, " +
                    "and save your changes."
            },

            new FaqItem
            {
                Question = "How can I change my password?",
                Answer =
                    "Open My Profile, select Change Password, enter your current " +
                    "password, choose a new password, confirm it, and save."
            }
            };

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public CustomerSupportPage()
        {
            InitializeComponent();

            BuildFaqList(_allFaqs);
        }

        // =========================================================
        // BUILD FAQ LIST
        // =========================================================

        private void BuildFaqList(
            IEnumerable<FaqItem> faqs)
        {
            FaqContainer.Children.Clear();

            foreach (FaqItem faq in faqs)
            {
                FaqContainer.Children.Add(
                    CreateFaqCard(faq));
            }
        }

        // =========================================================
        // CREATE FAQ CARD
        // =========================================================

        private View CreateFaqCard(
            FaqItem faq)
        {
            var questionLabel =
                new Label
                {
                    Text = faq.Question,
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    VerticalOptions =
                        LayoutOptions.Center
                };

            var arrowLabel =
                new Label
                {
                    Text = "⌄",
                    FontSize = 20,
                    TextColor =
                        Color.FromArgb("#AEB4BF"),
                    VerticalOptions =
                        LayoutOptions.Center,
                    HorizontalOptions =
                        LayoutOptions.End
                };

            var answerLabel =
                new Label
                {
                    Text = faq.Answer,
                    FontSize = 13,
                    TextColor =
                        Color.FromArgb("#A2A8B3"),
                    LineBreakMode =
                        LineBreakMode.WordWrap,
                    IsVisible = false
                };

            var header =
                new Grid
                {
                    ColumnDefinitions =
                    {
                    new ColumnDefinition(
                        GridLength.Star),

                    new ColumnDefinition(
                        GridLength.Auto)
                    }
                };

            header.Add(
                questionLabel,
                0,
                0);

            header.Add(
                arrowLabel,
                1,
                0);

            var content =
                new VerticalStackLayout
                {
                    Spacing = 12
                };

            content.Children.Add(header);
            content.Children.Add(answerLabel);

            var border =
                new Border
                {
                    BackgroundColor =
                        Color.FromArgb("#191C21"),

                    Stroke =
                        Color.FromArgb("#292D34"),

                    StrokeThickness = 1,

                    StrokeShape =
                        new RoundRectangle
                        {
                            CornerRadius =
                                new CornerRadius(15)
                        },

                    Padding =
                        new Thickness(16),

                    Content = content
                };

            var tapGesture =
                new TapGestureRecognizer();

            tapGesture.Tapped += async (_, _) =>
            {
                bool expanded =
                    !answerLabel.IsVisible;

                answerLabel.IsVisible =
                    expanded;

                arrowLabel.Text =
                    expanded
                        ? "⌃"
                        : "⌄";

                if (expanded)
                {
                    await border.FadeToAsync(
                        0.88,
                        80);

                    await border.FadeToAsync(
                        1.0,
                        80);
                }
            };

            border.GestureRecognizers.Add(
                tapGesture);

            return border;
        }

        // =========================================================
        // FAQ SEARCH
        // =========================================================

        private void OnSearchTextChanged(
            object? sender,
            TextChangedEventArgs e)
        {
            string searchText =
                e.NewTextValue?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    searchText))
            {
                BuildFaqList(_allFaqs);

                return;
            }

            var filteredFaqs =
                _allFaqs
                    .Where(f =>
                        f.Question.Contains(
                            searchText,
                            StringComparison
                                .OrdinalIgnoreCase)
                        ||
                        f.Answer.Contains(
                            searchText,
                            StringComparison
                                .OrdinalIgnoreCase))
                    .ToList();

            BuildFaqList(filteredFaqs);
        }

        // =========================================================
        // BACK
        // =========================================================

        private async void OnBackClicked(
            object? sender,
            EventArgs e)
        {
            await Navigation.PopAsync();
        }

        // =========================================================
        // CHAT SUPPORT
        // =========================================================

        private async void OnChatSupportClicked(
            object? sender,
            EventArgs e)
        {
            await DisplayAlertAsync(
                "Chat Support",
                "Support chat is available for booking and account assistance.\n\n" +
                "For this training project, please use the Email Support option " +
                "to send a support request.",
                "OK");
        }

        // =========================================================
        // EMAIL SUPPORT
        // =========================================================

        private async void OnEmailSupportClicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                var message =
                    new EmailMessage
                    {
                        Subject =
                            "IndoPlayHub Customer Support",

                        Body =
                            "Hello IndoPlayHub Support,\n\n" +
                            "I need assistance with:\n\n" +
                            "Booking ID (if applicable):\n\n" +
                            "Issue:\n\n" +
                            "Thank you."
                    };

                await Email.Default.ComposeAsync(
                    message);
            }
            catch
            {
                await DisplayAlertAsync(
                    "Email Support",
                    "No email application is available on this device. " +
                    "Please contact IndoPlayHub support using your configured support channel.",
                    "OK");
            }
        }
    }


}

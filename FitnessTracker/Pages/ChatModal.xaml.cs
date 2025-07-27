using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Controls;

namespace FitnessTrackingApp.Pages
{
    public partial class ChatModal : ContentPage
    {
        private readonly OpenRouterChatBot _chatBot;
        private bool _isBotTyping;

        public ChatModal()
        {
            InitializeComponent();

            string apiKey = ""; // API ключ от openrouter.ai
            _chatBot = new OpenRouterChatBot(apiKey);

            AddBotMessage("Привет! Я ваш виртуальный фитнес-тренер. Задавайте любые вопросы о тренировках, питании и здоровом образе жизни!");
        }

        private async void SendButton_Clicked(object sender, EventArgs e)
        {
            if (_isBotTyping) return;

            var userMessage = UserInput.Text?.Trim();
            if (string.IsNullOrWhiteSpace(userMessage)) return;

            AddUserMessage(userMessage);
            UserInput.Text = string.Empty;
            _isBotTyping = true;

            try
            {
                AddTypingIndicator();

                var response = await _chatBot.GetResponseAsync(userMessage);

                RemoveTypingIndicator();
                AddBotMessage(response);
            }
            catch (Exception ex)
            {
                RemoveTypingIndicator();
                AddSystemMessage($"Ошибка: {ex.Message}");
            }
            finally
            {
                _isBotTyping = false;
            }
        }

        private void AddUserMessage(string message)
        {
            AddMessage(message, isUser: true);
        }

        private void AddBotMessage(string message)
        {
            AddMessage(message, isUser: false);
        }

        private void AddSystemMessage(string message)
        {
            var frame = new Frame
            {
                Content = new Label { Text = message },
                BackgroundColor = Color.FromArgb("#FFEBEE"),
                HorizontalOptions = LayoutOptions.Center
            };

            ChatStack.Children.Add(frame);
            ScrollToBottom();
        }

        private void AddTypingIndicator()
        {
            var indicator = new ActivityIndicator
            {
                IsRunning = true,
                Color = (Color)Resources["PrimaryColor"],
                HorizontalOptions = LayoutOptions.Start,
                Margin = new Thickness(12, 8)
            };

            var frame = new Frame
            {
                Content = indicator,
                BackgroundColor = (Color)Resources["BotMessageColor"],
                HorizontalOptions = LayoutOptions.Start
            };

            ChatStack.Children.Add(frame);
            ScrollToBottom();
        }

        private void RemoveTypingIndicator()
        {
            if (ChatStack.Children.LastOrDefault() is Frame lastFrame &&
                lastFrame.Content is ActivityIndicator)
            {
                ChatStack.Children.Remove(lastFrame);
            }
        }

        private void AddMessage(string message, bool isUser)
        {
            var messageLabel = new Label
            {
                Text = message,
                FontSize = 14,
                LineBreakMode = LineBreakMode.WordWrap
            };

            var frame = new Frame
            {
                Content = messageLabel,
                BackgroundColor = isUser
                    ? (Color)Resources["UserMessageColor"]
                    : (Color)Resources["BotMessageColor"],
                HorizontalOptions = isUser ? LayoutOptions.End : LayoutOptions.Start
            };

            ChatStack.Children.Add(frame);
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                await ScrollView.ScrollToAsync(ScrollView, ScrollToPosition.End, true);
            });
        }

        private async void CloseButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }


    public class OpenRouterChatBot
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private const string ApiUrl = "https://openrouter.ai/api/v1/chat/completions";

        public OpenRouterChatBot(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/IvanZhutyaev/FitnessTrackingApp");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "FitnessTrackingAI");
        }

        public async Task<string> GetResponseAsync(string prompt)
        {
            var requestData = new
            {
                model = "mistralai/mistral-7b-instruct",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "Вы — квалифицированный фитнес-тренер. Ваша задача — давать чёткие, профессиональные и обоснованные ответы на вопросы, касающиеся сна, питания, физических тренировок, восстановления, режима дня, образа жизни и мотивации. Вы не отвечаете на вопросы, не связанные с данной тематикой. В таких случаях следует строго и сдержанно сообщить, что ваша компетенция ограничена вопросами, касающимися фитнеса, здоровья и тренировочного процесса."
                    },
                    new { role = "user", content = prompt }
                },
                max_tokens = 500,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(ApiUrl, content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                var choices = doc.RootElement.GetProperty("choices");
                var firstChoice = choices[0];
                var message = firstChoice.GetProperty("message");
                var contentValue = message.GetProperty("content").GetString();

                return contentValue?.Trim() ?? "Пустой ответ от модели.";
            }
            catch (Exception ex)
            {
                return $"Ошибка: {ex.Message}";
            }
        }
    }
}
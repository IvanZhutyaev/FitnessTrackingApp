using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace FitnessTrackingApp
{
    public partial class MainPage : ContentPage
    {
        private bool _isLoginMode = true;
        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "http://localhost:5024";
        private string _currentUsername = string.Empty;

        public MainPage()
        {
            InitializeComponent();
        }

        private void LoginButton_Clicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentUsername))
            {
                ResetAuthFields();
                SwitchToLoginMode();
                AuthPopup.IsVisible = true;
            }
            else
            {
                LoadAccountData();
                AccountPopup.IsVisible = true;
            }
        }

        private async void LoadAccountData()
        {
            AccountUsernameLabel.Text = $"Имя: {_currentUsername}";
            AccountStatsLabel.Text = "Загрузка статистики...";

            try
            {
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}/user/stats?username={_currentUsername}");
                if (response.IsSuccessStatusCode)
                {
                    var stats = await response.Content.ReadFromJsonAsync<UserStats>();
                    AccountStatsLabel.Text = $"Шаги: {stats?.AvgSteps ?? 0}/день\n" +
                                          $"Дистанция: {stats?.AvgDistance ?? 0:F1} км/день\n" +
                                          $"Калории: {stats?.AvgCalories ?? 0}/день";
                }
            }
            catch
            {
                AccountStatsLabel.Text = "Не удалось загрузить статистику";
            }
        }

        private void CloseAuthPopup_Clicked(object sender, EventArgs e)
        {
            AuthPopup.IsVisible = false;
        }

        private void SwitchAuthModeButton_Clicked(object sender, EventArgs e)
        {
            _isLoginMode = !_isLoginMode;
            ResetAuthFields();

            if (_isLoginMode)
            {
                SwitchToLoginMode();
            }
            else
            {
                SwitchToRegisterMode();
            }
        }

        private void ResetAuthFields()
        {
            UsernameEntry.Text = string.Empty;
            PasswordEntry.Text = string.Empty;
            ConfirmPasswordEntry.Text = string.Empty;
        }

        private void SwitchToLoginMode()
        {
            _isLoginMode = true;
            AuthPopupTitle.Text = "Вход в систему";
            AuthActionButton.Text = "Войти";
            ConfirmPasswordEntry.IsVisible = false;
            SwitchAuthModeButton.Text = "Нет аккаунта? Зарегистрируйтесь!";
            (AuthPopup.Content as Frame).HeightRequest = 450;
        }

        private void SwitchToRegisterMode()
        {
            _isLoginMode = false;
            AuthPopupTitle.Text = "Регистрация";
            AuthActionButton.Text = "Зарегистрироваться";
            ConfirmPasswordEntry.IsVisible = true;
            SwitchAuthModeButton.Text = "Уже есть аккаунт? Войти";
            (AuthPopup.Content as Frame).HeightRequest = 500;
        }

        private async void AuthActionButton_Clicked(object sender, EventArgs e)
        {
            var username = UsernameEntry.Text.Trim();
            var password = PasswordEntry.Text;

            if (string.IsNullOrWhiteSpace(username))
            {
                await DisplayAlert("Ошибка", "Введите имя пользователя", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Ошибка", "Введите пароль", "OK");
                return;
            }

            if (!_isLoginMode && password != ConfirmPasswordEntry.Text)
            {
                await DisplayAlert("Ошибка", "Пароли не совпадают", "OK");
                return;
            }

            try
            {
                if (_isLoginMode)
                {
                    await LoginUser(username, password);
                }
                else
                {
                    await RegisterUser(username, password);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Произошла ошибка: {ex.Message}", "OK");
            }
        }

        private async Task LoginUser(string username, string password)
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{ApiBaseUrl}/login",
                new { Username = username, Password = password });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResult>();
                if (result?.Success == true)
                {
                    _currentUsername = username;
                    UpdateUIAfterLogin();
                    await DisplayAlert("Успех", result.Message ?? "Вход выполнен успешно!", "OK");
                    AuthPopup.IsVisible = false;
                }
                else
                {
                    await DisplayAlert("Ошибка", result?.Message ?? "Неверные учетные данные", "OK");
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Ошибка", error ?? "Неверные учетные данные", "OK");
            }
        }

        private async Task RegisterUser(string username, string password)
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{ApiBaseUrl}/register",
                new { Username = username, Password = password });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResult>();
                await DisplayAlert("Успех", result?.Message ?? "Регистрация завершена успешно!", "OK");
                SwitchToLoginMode();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Ошибка", error ?? "Ошибка регистрации", "OK");
            }
        }

        private void UpdateUIAfterLogin()
        {
            LoginButton.Text = _currentUsername;
            LoginButton.FontSize = 16;
            LoginButton.FontAttributes = FontAttributes.Bold;
            LoginButton.BackgroundColor = Color.FromArgb("#1A3A6F");
            LoginButton.TextColor = Colors.White;
            LoginButton.CornerRadius = 20;
            LoginButton.Padding = new Thickness(15, 8);
            LoginButton.WidthRequest = 150;
        }

        private void LogoutButton_Clicked(object sender, EventArgs e)
        {
            _currentUsername = string.Empty;
            LoginButton.Text = "Вход";
            LoginButton.FontSize = 14;
            LoginButton.FontAttributes = FontAttributes.None;
            LoginButton.BackgroundColor = Color.FromArgb("#00C9FF");
            LoginButton.TextColor = Color.FromArgb("#0C1B33");
            LoginButton.CornerRadius = 15;
            LoginButton.Padding = new Thickness(10, 5);
            LoginButton.WidthRequest = 120;
            AccountPopup.IsVisible = false;
        }

        private void OnWorkoutButtonClicked(object sender, EventArgs e) => DisplayAlert("Тренировка", "Функция добавления тренировки", "OK");
        private void OnNutritionButtonClicked(object sender, EventArgs e) => DisplayAlert("Питание", "Функция добавления приема пищи", "OK");
        private void OnReminderButtonClicked(object sender, EventArgs e) => DisplayAlert("Напоминание", "Функция добавления напоминания", "OK");
        private void OnReportButtonClicked(object sender, EventArgs e) => DisplayAlert("Аналитика", "Функция показа отчетов", "OK");
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public class UserStats
    {
        public double AvgSteps { get; set; }
        public double AvgDistance { get; set; }
        public double AvgCalories { get; set; }
    }
}
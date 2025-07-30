using FitnessTrackingApp.Pages;
using Microsoft.Maui.Controls;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FitnessTrackingApp.Pages;
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
                // Вместо открытия модального окна - переход на страницу профиля
                Navigation.PushAsync(new ProfilePage());
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
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlert("Подтверждение выхода",
                "Вы действительно хотите выйти из аккаунта?",
                "Да", "Нет");

            if (answer)
            {
                // Очищаем данные сессии
                UserSession.UserId = 0;
                UserSession.Username = null;

                // Создаем новый экземпляр AppShell и устанавливаем его как MainPage
                var newShell = new AppShell();
                Application.Current.MainPage = newShell;

                // Находим MainPage в новом Shell и обновляем UI
                if (newShell.CurrentPage is MainPage mainPage)
                {
                    mainPage.UpdateUIAfterLogout();
                }
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
                    UserSession.Username = username;

                    var userResponse = await _httpClient.GetAsync($"{ApiBaseUrl}/users/{username}");
                    if (userResponse.IsSuccessStatusCode)
                    {
                        var user = await userResponse.Content.ReadFromJsonAsync<User>();
                        UserSession.UserId = user.Id;
                    }

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
            // Формируем дату рождения из пикеров
            string birthDay = (regBirthDayPicker.SelectedIndex + 1).ToString("D2");
            string[] months = { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };
            string birthMonth = (regBirthMonthPicker.SelectedIndex + 1).ToString("D2");
            string birthYear = (1950 + regBirthYearPicker.SelectedIndex).ToString();
            string birthDate = $"{birthDay}.{birthMonth}.{birthYear}";

            // Пример дополнительных полей (если будут)
            // double weight = 0;
            // if (!string.IsNullOrWhiteSpace(WeightEntry.Text) && double.TryParse(WeightEntry.Text, out double w))
            //     weight = w;

            var regData = new {
                Username = username,
                Password = password,
                BirthDate = birthDate
                
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{ApiBaseUrl}/register",
                regData);

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
            LoginButton.BackgroundColor = Color.FromArgb("#0d1a32ff");
            LoginButton.TextColor = Colors.White;
            LoginButton.CornerRadius = 20;
            LoginButton.Padding = new Thickness(15, 8);
            LoginButton.WidthRequest = 150;
        }
        public void UpdateUIAfterLogout()
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
        }



        // Новые обработчики для навигации по разделам
        private async void OnProfileTapped(object sender, EventArgs e)
        {
            if (UserSession.UserId > 0)
            {
                await Navigation.PushAsync(new ProfilePage());
            }
            else
            {
                await DisplayAlert("Ошибка", "Сначала выполните вход в систему", "OK");
            }

        }

        private async void OnWorkoutsTapped(object sender, EventArgs e)
        {
            if (UserSession.UserId > 0)
            {
                await Navigation.PushAsync(new WorkoutsPage());
            }
            else
            {
                await DisplayAlert("Ошибка", "Сначала выполните вход в систему", "OK");
            }
        
        }

        private async void OnActivityTapped(object sender, EventArgs e)
        {
            //await DisplayAlert("Активность", "Переход на страницу активности", "OK");
            if (UserSession.UserId > 0)
            {
                var activityPage = Handler.MauiContext.Services.GetService<ActivityPage>();
                await Navigation.PushAsync(activityPage);
            }
            else
            {
                await DisplayAlert("Ошибка", "Сначала выполните вход в систему", "OK");
            }
            
            
        }

        private async void OnNutritionTapped(object sender, EventArgs e)
        {
            //await DisplayAlert("Питание", "Переход на страницу питания", "OK");
            if (UserSession.UserId > 0)
            {
                await Navigation.PushAsync(new Pages.NutritionPage());
            }
            else
            {
                await DisplayAlert("Ошибка", "Сначала выполните вход в систему", "OK");
            }
        }

        private async void OnProgressTapped(object sender, EventArgs e)
        {
            //await DisplayAlert("Прогресс", "Переход на страницу прогресса", "OK");
            if (UserSession.UserId > 0)
            {
                await Navigation.PushAsync(new Pages.ProgressPage());
            }
            else
            {
                await DisplayAlert("Ошибка", "Сначала выполните вход в систему", "OK");
            }
        }

        private async void OnNotificationsTapped(object sender, EventArgs e)
        {
            //await DisplayAlert("Уведомления", "Переход на страницу уведомлений", "OK");
            await Navigation.PushAsync(new Pages.NotificationsPage(UserSession.UserId));
        }

        private async void OnAdditionallyTapped(object sender, EventArgs e)
        {
            //await DisplayAlert("Настройки", "Переход на страницу настроек", "OK");
            await Navigation.PushAsync(new Pages.AdditionallyPage());
        }
        private async void OpenChatButton_Clicked(object sender, EventArgs e)
        {
            if (UserSession.UserId > 0)
            {
                var chatModal = new ChatModal();
                await Navigation.PushModalAsync(chatModal);
            }
            else
            {
                await DisplayAlert("Ошибка", "Сначала выполните вход в систему", "OK");
            }

        }
    }

    // Глобальный статический класс для хранения данных пользователя
    public static class UserSession
    {
        public static int UserId { get; set; }
        public static string Username { get; set; } = string.Empty;
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


    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int Age { get; set; }
        public double Weight { get; set; }
        public double Height { get; set; }
    }
}
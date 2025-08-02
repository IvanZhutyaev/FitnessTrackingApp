using FitnessTrackingApp.Pages;
using Microsoft.Maui.Controls;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FitnessTrackingApp.Pages;
using Microsoft.Maui.Controls;
using System.Linq;

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
            UpdateUIifUserExists();
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
                UserSession.UserId = 0;
                UserSession.Username = null;

                var newShell = new AppShell();
                Application.Current.MainPage = newShell;

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

                    await UpdateStaticUserData(UserSession.Username);
                    UpdateUIAfterLogin();
                    await DisplayAlert("Успех", result.Message ?? "Вход выполнен успешно!", "OK");
                    AuthPopup.IsVisible = false;
                    try
                    {

                        await SecureStorage.SetAsync("username", username); //Сохранение в SecureStorage
                        var testSave = await SecureStorage.GetAsync("username");
                        await DisplayAlert("Данные", $"Данные {testSave} сохранены", "ОК");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", ex.Message, "OK");
                    }

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
            string birthDay = (regBirthDayPicker.SelectedIndex + 1).ToString("D2");
            string[] months = { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };
            string birthMonth = (regBirthMonthPicker.SelectedIndex + 1).ToString("D2");
            string birthYear = (1950 + regBirthYearPicker.SelectedIndex).ToString();
            string birthDate = $"{birthDay}.{birthMonth}.{birthYear}";

            var regData = new
            {
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

        private async Task UpdateUIifUserExists()
        {
            var storedUsername = await SecureStorage.GetAsync("username");
            if (!string.IsNullOrEmpty(storedUsername) && UserSession.Username == string.Empty)
            {

                DisplayAlert("Успех", "Данные пользователя имеются в SecureStorage", "Заебись");
                _currentUsername = await SecureStorage.GetAsync("username");
                UpdateUIAfterLogin();
            }
            else
                        <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">
            <dict>
                <key>keychain-access-groups</key>
                <array>
                    <string>$(AppIdentifierPrefix)$(CFBundleIdentifier)</string>
                </array>
            </dict>
            </plist>{

                DisplayAlert("Ошибка", "Данные не удалось получить", "Заебись");

            }
        }


        //Респонз для получения шагов. Пока только шагов


        private async Task UpdateStaticUserData(string username)
        {
            try
            {

                var userResponse = await _httpClient.GetAsync($"{ApiBaseUrl}/activities/stats/{username}");
                if (userResponse.IsSuccessStatusCode)
                {
                    var user = await userResponse.Content.ReadFromJsonAsync<List<Activity>>();

                    if (user.Any())
                    {

                        UserStaticData.AvgDistance = user.Average(d => d.Distance);
                        UserStaticData.AvgCalories = user.Average(c => c.Calories);
                        UserStaticData.Steps = user.Sum(s => s.Steps);
                        UserStaticData.AvgSteps = user.Average(s => s.Steps);

                        await DisplayAlert("Успешно", "Данные о шагах получены!", "ОК");
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", "Данные о шагах отсутсвуют", "ОК");
                    }
                }

            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", "При получении данных пользоваетля произошла ошибка", "ОК");
            }
        }

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
            await Navigation.PushAsync(new Pages.NotificationsPage(UserSession.UserId));
        }

        private async void OnAdditionallyTapped(object sender, EventArgs e)
        {
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

    public class Activity
    {
        public int UserId { get; set; }
        public int Steps { get; set; }
        public DateTime Date { get; set; }
        public double Distance { get; set; }
        public double Calories { get; set; }
    }

    public static class UserStaticData
    {
        public static int Steps { get; set; }
        public static double AvgSteps { get; set; }
        public static double AvgDistance { get; set; }
        public static double AvgCalories { get; set; }
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

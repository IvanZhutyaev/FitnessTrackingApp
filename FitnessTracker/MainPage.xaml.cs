using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace FitnessTrackingApp
{
    public partial class MainPage : ContentPage
    {
        private bool _isLoginMode = true;

        public MainPage()
        {
            InitializeComponent();
        }

        private void LoginButton_Clicked(object sender, EventArgs e)
        {
            // Сбрасываем поля при открытии
            UsernameEntry.Text = string.Empty;
            PasswordEntry.Text = string.Empty;
            ConfirmPasswordEntry.Text = string.Empty;

            // Устанавливаем режим входа
            _isLoginMode = true;
            AuthPopupTitle.Text = "Вход в систему";
            AuthActionButton.Text = "Войти";
            ConfirmPasswordEntry.IsVisible = false;
            SwitchAuthModeButton.Text = "Нет аккаунта? Зарегистрироваться";

            // Устанавливаем высоту окна
            if (AuthPopup.Content is Frame frame)
            {
                frame.HeightRequest = 340;
            }

            // Показываем модальное окно
            AuthPopup.IsVisible = true;
        }

        private void CloseAuthPopup_Clicked(object sender, EventArgs e)
        {
            AuthPopup.IsVisible = false;
        }

        private void SwitchAuthModeButton_Clicked(object sender, EventArgs e)
        {
            _isLoginMode = !_isLoginMode;

            if (_isLoginMode)
            {
                // Переключаемся в режим входа
                AuthPopupTitle.Text = "Вход в систему";
                AuthActionButton.Text = "Войти";
                ConfirmPasswordEntry.IsVisible = false;
                SwitchAuthModeButton.Text = "Нет аккаунта? Зарегистрироваться";

                // Уменьшаем высоту окна
                if (AuthPopup.Content is Frame frame)
                {
                    frame.HeightRequest = 340;
                }
            }
            else
            {
                // Переключаемся в режим регистрации
                AuthPopupTitle.Text = "Регистрация";
                AuthActionButton.Text = "Зарегистрироваться";
                ConfirmPasswordEntry.IsVisible = true;
                SwitchAuthModeButton.Text = "Уже есть аккаунт? Войти";

                // Увеличиваем высоту окна
                if (AuthPopup.Content is Frame frame)
                {
                    frame.HeightRequest = 400;
                }
            }
        }

        private async void AuthActionButton_Clicked(object sender, EventArgs e)
        {
            var username = UsernameEntry.Text;
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

            // Здесь должна быть реальная логика аутентификации
            // В данном примере просто имитируем успешный вход/регистрацию
            if (_isLoginMode)
            {
                // Имитация входа
                await Task.Delay(500);
                await DisplayAlert("Успех", "Вход выполнен успешно!", "OK");
                LoginButton.Text = username;
                LoginButton.BackgroundColor = Colors.Transparent;
                LoginButton.TextColor = Colors.White;
                AuthPopup.IsVisible = false;
            }
            else
            {
                // Имитация регистрации
                await Task.Delay(500);
                await DisplayAlert("Успех", "Регистрация завершена успешно!", "OK");

                // Переключаемся в режим входа после успешной регистрации
                _isLoginMode = true;
                AuthPopupTitle.Text = "Вход в систему";
                AuthActionButton.Text = "Войти";
                ConfirmPasswordEntry.IsVisible = false;
                SwitchAuthModeButton.Text = "Нет аккаунта? Зарегистрироваться";

                if (AuthPopup.Content is Frame frame)
                {
                    frame.HeightRequest = 340;
                }
            }
        }

        // Обработчики кнопок функционала приложения
        private void OnWorkoutButtonClicked(object sender, EventArgs e)
        {
            DisplayAlert("Тренировка", "Функция добавления тренировки", "OK");
        }

        private void OnNutritionButtonClicked(object sender, EventArgs e)
        {
            DisplayAlert("Питание", "Функция добавления приема пищи", "OK");
        }

        private void OnReminderButtonClicked(object sender, EventArgs e)
        {
            DisplayAlert("Напоминание", "Функция добавления напоминания", "OK");
        }

        private void OnReportButtonClicked(object sender, EventArgs e)
        {
            DisplayAlert("Аналитика", "Функция показа отчетов", "OK");
        }
    }
}
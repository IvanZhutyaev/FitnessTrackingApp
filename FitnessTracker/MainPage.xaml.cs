using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;


namespace FitnessTrackingApp
{
    public partial class MainPage : ContentPage
    {

        public MainPage()
        {
            InitializeComponent();
        }

        private async void LoginSubmitButton_Clicked(object sender, EventArgs e)
        {
            var username = LoginEntry.Text;
            var password = PasswordEntry.Text;

            var httpClient = new HttpClient();
            var request = new { Username = username, Password = password };

            try
            {
                var response = await httpClient.PostAsJsonAsync("http://localhost:5024/login", request);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Вход в систему", "Вы успешно вошли в аккаунт!", "OK");
                    RegisterPopup.IsVisible = false;
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Ошибка", errorMessage, "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Произошла ошибка: {ex.Message}", "OK");
            }

        }
        private void RegisterSubmitButton_Clicked(object sender, EventArgs e)
        {
            RegisterPopup.IsVisible = true;
        }

        private void RegisterButton_Clicked(object sender, EventArgs e)
        {
            RegisterPopup.IsVisible = true;
            
        }

        private void CloseRegisterPopup_Clicked(object sender, EventArgs e)
        {
            RegisterPopup.IsVisible = false;
        }

        // Заглушки для обработки событий (можно реализовать позже)
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

        private void SwitchAuthModeButton_Clicked(object sender, EventArgs e)
        {
            DisplayAlert("Переключение режима", "Функция переключения между регистрацией и входом", "OK");
        }

        
    }
}
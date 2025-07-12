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




        //<-------------------------------------Первый блок обратботки (вход и регистрация)
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
                    await DisplayAlert("Вход", "Пользователь успешно вошел в систему!", "OK");
                    LoginPopup.IsVisible = false; // Закрыть окно входа
                    // Здесь можно добавить логику перехода на главный экран приложения
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


        private async void RegisterSubmitButton_Clicked(object sender, EventArgs e)
        {
            var username = LoginEntry.Text;
            var password = PasswordEntry.Text;

            var httpClient = new HttpClient();
            var request = new { Username = username, Password = password };

            try
            {
                var response = await httpClient.PostAsJsonAsync("http://localhost:5024/register", request);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Регистрация", "Пользователь успешно зарегистрирован!", "OK");
                    //RegisterPopup.IsVisible = false; -- расскоментировать при реализации регистрации
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
        private void LoginButton_Clicked(object sender, EventArgs e) // открытие окна входа
        {
            //RegisterPopup.IsVisible = false; -- Закрыть окно регистрации, если оно открыто
            LoginPopup.IsVisible = true;
            
        }


        private void CloseLoginPopup_Clicked(object sender, EventArgs e)
        {
            LoginPopup.IsVisible = false;
        }
        private void CloseRegisterPopup_Clicked(object sender, EventArgs e)
        {
            //RegisterPopup.IsVisible = false;  -- Закрыть окно регистрации
            DisplayAlert("", "Функция закрытия окна регистрации", "OK");
        }

        private void SwitchAuthModeButton_Clicked(object sender, EventArgs e)
        {

            DisplayAlert("Смена режима", "Функция смены режима авторизации", "OK");
        }






        //<-------------------------------------Второй блок обратботки (основные функции приложения)

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

        
    }
}
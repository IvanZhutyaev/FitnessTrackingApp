namespace FitnessTrackingApp
{
    public partial class MainPage : ContentPage
    {

        public MainPage()
        {
            InitializeComponent();
        }

        // Открыть окно регистрации
        private void RegisterButton_Clicked(object sender, EventArgs e)
        {
            RegisterPopup.IsVisible = true;
        }

        // Закрыть окно регистрации
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
    }
}
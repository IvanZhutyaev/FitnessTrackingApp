using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FitnessTrackingApp.Pages
{
    public partial class ProfilePage : ContentPage
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "http://localhost:5024";

        // Список целей для Picker
        public List<string> Goals { get; } = new List<string>
        {
            "Похудение",
            "Набор массы",
            "Поддержание формы",
            "Увеличение выносливости"
        };

        public ProfilePage()
        {
            InitializeComponent();
            goalPicker.ItemsSource = Goals;
            LoadUserDataAsync(); // Изменили на асинхронную версию
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadUserDataAsync(); // Добавили await
        }

        private async Task LoadUserDataAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.Username))
                {
                    await DisplayAlert("Ошибка", "Пользователь не авторизован", "OK");
                    return;
                }

                var response = await _httpClient.GetAsync($"{ApiBaseUrl}/users/profile/{UserSession.Username}");

                if (response.IsSuccessStatusCode)
                {
                    var profileData = await response.Content.ReadFromJsonAsync<UserProfileDto>();

                    usernameEntry.Text = profileData.Username;
                    ageEntry.Text = profileData.Age.ToString();
                    heightEntry.Text = profileData.Height.ToString();
                    weightEntry.Text = profileData.Weight.ToString();
                    targetWeightEntry.Text = profileData.TargetWeight.ToString(); // Обновляем Entry вместо Label

                    goalPicker.SelectedItem = profileData.Goal ?? "Похудение";
                    targetPeriodLabel.Text = profileData.TargetPeriod ?? "3 месяца";
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось загрузить данные профиля", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async void OnSaveProfileChangesClicked(object sender, EventArgs e)
        {
            try
            {
                var profileData = new UserProfileDto
                {
                    Username = UserSession.Username,
                    Age = int.Parse(ageEntry.Text),
                    Height = double.Parse(heightEntry.Text),
                    Weight = double.Parse(weightEntry.Text),
                    Goal = goalPicker.SelectedItem?.ToString(),
                    TargetWeight = double.Parse(targetWeightEntry.Text), // Получаем значение из Entry
                    TargetPeriod = targetPeriodLabel.Text
                };

                var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/user/updateprofile", profileData);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Успех", "Профиль успешно обновлен", "OK");
                    await LoadUserDataAsync();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Ошибка", error, "OK");
                }
            }
            catch (FormatException)
            {
                await DisplayAlert("Ошибка", "Проверьте правильность введенных чисел", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }



        public class UserProfileDto
        {
            public string Username { get; set; } = string.Empty;
            public int Age { get; set; }
            public double Weight { get; set; }
            public double Height { get; set; }
            public string Goal { get; set; } = string.Empty;
            public double TargetWeight { get; set; }
            public string TargetPeriod { get; set; } = string.Empty;
        }
    }
}
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FitnessTrackingApp.Pages
{
    public partial class ProfilePage : ContentPage
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "http://localhost:5024";

        public ProfilePage()
        {
            InitializeComponent();
            LoadUserDataAsync();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadUserDataAsync();
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

                    // Основные данные
                    usernameEntry.Text = profileData.Username;
                    ageEntry.Text = profileData.Age.ToString();
                    heightEntry.Text = profileData.Height.ToString();
                    weightEntry.Text = profileData.Weight.ToString();
                    goalLabel.Text = profileData.Goal ?? "Не указана";

                    // Параметры целей
                    targetWeightEntry.Text = profileData.TargetWeight.ToString();

                    if (!string.IsNullOrEmpty(profileData.TargetPeriod))
                    {
                        var periodOptions = new List<string> { "1 месяц", "3 месяца", "6 месяцев", "1 год" };
                        var periodIndex = periodOptions.IndexOf(profileData.TargetPeriod);
                        targetPeriodPicker.SelectedIndex = periodIndex >= 0 ? periodIndex : 1;
                    }
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
                    Goal = goalLabel.Text,
                    TargetWeight = double.Parse(targetWeightEntry.Text),
                    TargetPeriod = targetPeriodPicker.SelectedItem?.ToString() ?? "3 месяца"
                };

                var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/user/updateprofile", profileData);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Успех", "Данные сохранены", "OK");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Ошибка", error, "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }
    }

    public class UserProfileDto
    {
        public string Username { get; set; } = string.Empty;
        public int Age { get; set; }
        public double Weight { get; set; }
        public double Height { get; set; }
        public string Goal { get; set; } = "Похудение";
        public double TargetWeight { get; set; }
        public string TargetPeriod { get; set; } = "3 месяца";
    }
}
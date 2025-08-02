using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;


namespace FitnessTrackingApp.Pages
{
    public partial class ProfilePage : ContentPage
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "http://localhost:5024";

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
            LoadUserDataAsync();
        }

        private string _photoPath;

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadUserDataAsync();
            await LoadUserPhotoAsync();
        }

        private async Task LoadUserPhotoAsync()
        {
            try
            {
                if (UserSession.UserId == 0) return;

                var fileName = $"{UserSession.UserId}.jpg";
                var localFilePath = Path.Combine(FileSystem.AppDataDirectory, "Media", fileName);

                if (File.Exists(localFilePath))
                {
                    _photoPath = localFilePath;
                    avatarImage.Source = ImageSource.FromFile(localFilePath);
                }
                else
                {
                    avatarImage.Source = "user_avatar.png";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки фото: {ex.Message}");
            }
        }

        private async void OnChangePhotoClicked(object sender, EventArgs e)
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.Photos>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Ошибка", "Необходимо разрешение на доступ к фото", "OK");
                    return;
                }

                var result = await MediaPicker.PickPhotoAsync();
                if (result != null)
                {
                    var mediaFolder = Path.Combine(FileSystem.AppDataDirectory, "Media");
                    if (!Directory.Exists(mediaFolder))
                    {
                        Directory.CreateDirectory(mediaFolder);
                    }

                    var oldFilePath = Path.Combine(mediaFolder, $"{UserSession.UserId}.jpg");
                    if (File.Exists(oldFilePath))
                    {
                        File.Delete(oldFilePath);
                    }

                    var newFilePath = Path.Combine(mediaFolder, $"{UserSession.UserId}.jpg");
                    using (var stream = await result.OpenReadAsync())
                    using (var newStream = File.OpenWrite(newFilePath))
                    {
                        await stream.CopyToAsync(newStream);
                    }

                    _photoPath = newFilePath;
                    avatarImage.Source = ImageSource.FromFile(newFilePath);

                    await DisplayAlert("Успех", "Фото профиля обновлено", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось обновить фото: {ex.Message}", "OK");
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
                UserStaticData.Steps = 0;
                UserStaticData.AvgCalories = 0;
                UserStaticData.AvgSteps = 0;
                UserStaticData.AvgDistance = 0;

                var newShell = new AppShell();
                Application.Current.MainPage = newShell;

                if (newShell.CurrentPage is MainPage mainPage)
                {
                    mainPage.UpdateUIAfterLogout();
                }
            }
        }

        private void OnGoalPickerChanged(object sender, EventArgs e)
        {
            if (goalPicker.SelectedIndex >= 0)
            {
                goalLabel.Text = Goals[goalPicker.SelectedIndex];
            }
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
                    heightEntry.Text = profileData.Height.ToString();
                    weightEntry.Text = profileData.Weight.ToString();

                    if (!string.IsNullOrEmpty(profileData.BirthDate))
                    {
                        var parts = profileData.BirthDate.Split('.');
                        if (parts.Length == 3 && int.TryParse(parts[2], out int year) && int.TryParse(parts[1], out int month) && int.TryParse(parts[0], out int day))
                        {
                            var birthDate = new DateTime(year, month, day);
                            var today = DateTime.Today;
                            int age = today.Year - birthDate.Year;
                            if (birthDate > today.AddYears(-age)) age--;
                            birthDateEntry.Text = age.ToString();
                        }
                        else
                        {
                            birthDateEntry.Text = "";
                        }
                    }
                    else
                    {
                        birthDateEntry.Text = "";
                    }


                    if (!string.IsNullOrEmpty(profileData.Goal))
                    {
                        goalLabel.Text = profileData.Goal;
                        var goalIndex = Goals.IndexOf(profileData.Goal);
                        goalPicker.SelectedIndex = goalIndex >= 0 ? goalIndex : 0;
                    }

                    targetWeightEntry.Text = profileData.TargetWeight.ToString();

                    if (!string.IsNullOrEmpty(profileData.TargetPeriod))
                    {
                        var periodOptions = new List<string> { "1 месяц", "3 месяца", "6 месяцев", "1 год" };
                        var periodIndex = periodOptions.IndexOf(profileData.TargetPeriod);
                        targetPeriodPicker.SelectedIndex = periodIndex >= 0 ? periodIndex : 1;
                    }
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
            catch (FormatException)
            {
                await DisplayAlert("Ошибка", "Проверьте правильность введенных чисел", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }
    }

    public class UserProfileDto
    {
        public string Username { get; set; } = UserSession.Username ?? string.Empty;
        public double Weight { get; set; }
        public double Height { get; set; }
        public string Goal { get; set; } = "Похудение";
        public string BirthDate { get; set; } = "01.01.2000";
        public double TargetWeight { get; set; }
        public string TargetPeriod { get; set; } = "3 месяца";
    }
}

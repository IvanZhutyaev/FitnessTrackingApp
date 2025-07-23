using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System.Net.Http;
using System.Net.Http.Json;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace FitnessTrackingApp.Pages
{
    // ��������� ������ ����� � ���� �����
    public enum NotificationType
    {
        Meal,
        Workout,
        Water,
        Custom
    }

    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TimeSpan Time { get; set; }
        public bool IsActive { get; set; } = true;
        public NotificationType Type { get; set; }
    }

    public class NotificationSettings
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public bool MotivationalEnabled { get; set; } = true;
        public bool AchievementsEnabled { get; set; } = true;
        public bool ProgressEnabled { get; set; } = true;
        public bool GeneralEnabled { get; set; } = true;
        public bool SoundEnabled { get; set; } = true;
        public bool VibrationEnabled { get; set; } = true;
        public TimeSpan NotificationTime { get; set; } = new TimeSpan(9, 0, 0);
    }

    public partial class NotificationsPage : ContentPage
    {
        private readonly HttpClient _httpClient = new();
        private const string ApiBaseUrl = "http://localhost:5024";

        private int _userId;
        private List<Notification> _notifications = new();
        private NotificationSettings _settings = new();

        private readonly Dictionary<string, List<string>> _motivationalMessages = new()
        {
            ["Motivational"] = new List<string>
            {
                "�� ������� ��� �� ��� ����� � ����� ����!",
                "��������� ���� �������� � ������� �����������!",
                "�����, ������ �� ����� - ��� ������� �� ���������!"
            },
            ["Achievements"] = new List<string>
            {
                "�����������! �� �������� ����� ����!",
                "�������� ������! �� ��������� ��������� ����!",
                "����� ������! �� ��������� ����!"
            },
            ["Progress"] = new List<string>
            {
                "�� ������ �� ������ �� 15% ������!",
                "��� �������� ���������� - ����������� � ��� �� ����!",
                "������ �������, ��� �� ����� ���������� �� 20%"
            }
        };

        public NotificationsPage(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _httpClient.BaseAddress = new Uri(ApiBaseUrl);
            LoadData();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadData();
        }

        private async void LoadData()
        {
            await LoadNotifications();
            await LoadSettings();
        }

        private async Task LoadNotifications()
        {
            try
            {
                var response = await _httpClient.GetAsync($"/notifications/{_userId}");
                if (response.IsSuccessStatusCode)
                {
                    _notifications = await response.Content.ReadFromJsonAsync<List<Notification>>() ?? new();
                    UpdateNotificationsUI();
                }
                else
                {
                    await DisplayAlert("������", "�� ������� ��������� �����������", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("������", $"�� ������� ��������� �����������: {ex.Message}", "OK");
            }
        }

        private async Task LoadSettings()
        {
            try
            {
                var response = await _httpClient.GetAsync($"/notifications/settings/{_userId}");
                if (response.IsSuccessStatusCode)
                {
                    _settings = await response.Content.ReadFromJsonAsync<NotificationSettings>() ?? new();

                    MotivationalSwitch.IsToggled = _settings.MotivationalEnabled;
                    AchievementsSwitch.IsToggled = _settings.AchievementsEnabled;
                    ProgressSwitch.IsToggled = _settings.ProgressEnabled;
                    GeneralSwitch.IsToggled = _settings.GeneralEnabled;
                    SoundSwitch.IsToggled = _settings.SoundEnabled;
                    VibrationSwitch.IsToggled = _settings.VibrationEnabled;
                }
                else
                {
                    await DisplayAlert("������", "�� ������� ��������� ���������", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("������", $"�� ������� ��������� ���������: {ex.Message}", "OK");
            }
        }

        private void UpdateNotificationsUI()
        {
            NotificationsContainer.Children.Clear();

            foreach (var notification in _notifications.OrderBy(n => n.Time))
            {
                var grid = new Grid { ColumnSpacing = 15, Margin = new Thickness(0, 10) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var timeFrame = new Frame
                {
                    CornerRadius = 10,
                    BackgroundColor = Color.FromArgb("#1A3A6F"),
                    Padding = 10,
                    HeightRequest = 60
                };

                var timeLabel = new Label
                {
                    Text = notification.Time.ToString(@"hh\:mm"),
                    FontSize = 18,
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center
                };
                timeFrame.Content = timeLabel;

                var timeTap = new TapGestureRecognizer();
                timeTap.Tapped += async (s, e) => await ChangeNotificationTime(notification, timeLabel);
                timeFrame.GestureRecognizers.Add(timeTap);

                var contentStack = new VerticalStackLayout { Spacing = 5 };
                contentStack.Add(new Label
                {
                    Text = notification.Title,
                    TextColor = Colors.White,
                    FontAttributes = FontAttributes.Bold
                });
                contentStack.Add(new Label
                {
                    Text = notification.Description,
                    TextColor = Color.FromArgb("#5D8CC0")
                });

                var switchControl = new Switch
                {
                    IsToggled = notification.IsActive,
                    HorizontalOptions = LayoutOptions.End
                };
                switchControl.Toggled += async (s, e) => await OnNotificationToggled(notification, e.Value);

                grid.Add(timeFrame, 0, 0);
                grid.Add(contentStack, 1, 0);
                grid.Add(switchControl, 2, 0);

                NotificationsContainer.Children.Add(grid);
            }
        }

        private async Task ChangeNotificationTime(Notification notification, Label timeLabel)
        {
            var result = await DisplayPromptAsync(
                "�������� �����",
                "������� ����� ����� � ������� HH:mm",
                "���������",
                "������",
                keyboard: Keyboard.Numeric);

            if (!string.IsNullOrWhiteSpace(result))
            {
                if (TimeSpan.TryParse(result, out var newTime))
                {
                    notification.Time = newTime;
                    timeLabel.Text = newTime.ToString(@"hh\:mm");

                    var response = await _httpClient.PutAsJsonAsync(
                        $"/notifications/{notification.Id}", notification);

                    if (response.IsSuccessStatusCode)
                    {
                        await Toast.Make("����� ����������� ��������", ToastDuration.Short).Show();
                    }
                    else
                    {
                        await DisplayAlert("������", "�� ������� ��������� ���������", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("������", "������������ ������ �������", "OK");
                }
            }
        }

        private async void OnAddNotificationClicked(object sender, EventArgs e)
        {
            var result = await DisplayActionSheet(
                "�������� �����������",
                "������",
                null,
                "����� ����", "����������", "����", "������");

            if (result != "������" && result != null)
            {
                var timeResult = await DisplayPromptAsync(
                    "������� �����",
                    "������� ����� � ������� HH:mm",
                    "��������",
                    "������",
                    keyboard: Keyboard.Numeric);

                if (!string.IsNullOrWhiteSpace(timeResult) &&
                    TimeSpan.TryParse(timeResult, out var time))
                {
                    var newNotification = new Notification
                    {
                        UserId = _userId,
                        Title = result,
                        Description = GetDefaultDescription(result),
                        Time = time,
                        Type = GetNotificationType(result),
                        IsActive = true
                    };

                    var response = await _httpClient.PostAsJsonAsync(
                        "/notifications", newNotification);

                    if (response.IsSuccessStatusCode)
                    {
                        var createdNotification = await response.Content.ReadFromJsonAsync<Notification>();
                        _notifications.Add(createdNotification);
                        UpdateNotificationsUI();
                        await Toast.Make("����������� ���������", ToastDuration.Short).Show();
                    }
                    else
                    {
                        await DisplayAlert("������", "�� ������� �������� �����������", "OK");
                    }
                }
            }
        }

        private async Task OnNotificationToggled(Notification notification, bool isActive)
        {
            notification.IsActive = isActive;

            var response = await _httpClient.PutAsJsonAsync(
                $"/notifications/{notification.Id}/status", isActive);

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("������", "�� ������� �������� ������ �����������", "OK");
                notification.IsActive = !isActive; // ���������� ���������
                UpdateNotificationsUI();
            }
        }

        private async void OnViewMessagesClicked(object sender, EventArgs e)
        {
            var category = await DisplayActionSheet(
                "�������� ��� ���������",
                "������",
                null,
                "�������������", "����������", "��������");

            if (category != null && category != "������")
            {
                var key = category switch
                {
                    "�������������" => "Motivational",
                    "����������" => "Achievements",
                    "��������" => "Progress",
                    _ => ""
                };

                if (!string.IsNullOrEmpty(key) && _motivationalMessages.ContainsKey(key))
                {
                    var message = string.Join("\n\n", _motivationalMessages[key]);
                    await DisplayAlert($"������� {category.ToLower()} ���������", message, "�������");
                }
            }
        }

        private async void OnSelectTimeClicked(object sender, EventArgs e)
        {
            var result = await DisplayPromptAsync(
                "����� ������� �����������",
                "������� ����� � ������� HH:mm",
                "���������",
                "������",
                keyboard: Keyboard.Numeric);

            if (!string.IsNullOrWhiteSpace(result))
            {
                if (TimeSpan.TryParse(result, out var time))
                {
                    _settings.NotificationTime = time;
                    await SaveSettings();
                    await Toast.Make($"����� ����������� ����������� �� {time:hh\\:mm}", ToastDuration.Short).Show();
                }
                else
                {
                    await DisplayAlert("������", "������������ ������ �������", "OK");
                }
            }
        }

        private async void OnSettingToggled(object sender, ToggledEventArgs e)
        {
            var switchControl = (Switch)sender;
            var settingName = switchControl.StyleId;

            switch (settingName)
            {
                case "Motivational":
                    _settings.MotivationalEnabled = e.Value;
                    break;
                case "Achievements":
                    _settings.AchievementsEnabled = e.Value;
                    break;
                case "Progress":
                    _settings.ProgressEnabled = e.Value;
                    break;
                case "General":
                    _settings.GeneralEnabled = e.Value;
                    break;
                case "Sound":
                    _settings.SoundEnabled = e.Value;
                    break;
                case "Vibration":
                    _settings.VibrationEnabled = e.Value;
                    break;
            }

            await SaveSettings();
        }

        private async Task SaveSettings()
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(
                    $"/notifications/settings/{_userId}", _settings);

                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlert("������", "�� ������� ��������� ���������", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("������", $"�� ������� ��������� ���������: {ex.Message}", "OK");
            }
        }

        private string GetDefaultDescription(string title)
        {
            return title switch
            {
                "����� ����" => "�������� ����� ����",
                "����������" => "��������������� ����������",
                "����" => "������ ����",
                _ => "������������������� �����������"
            };
        }

        private NotificationType GetNotificationType(string title)
        {
            return title switch
            {
                "����� ����" => NotificationType.Meal,
                "����������" => NotificationType.Workout,
                "����" => NotificationType.Water,
                _ => NotificationType.Custom
            };
        }
    }
}
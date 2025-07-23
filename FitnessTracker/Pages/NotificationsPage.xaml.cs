using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace FitnessTrackingApp.Pages
{
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
    }

    public partial class NotificationsPage : ContentPage
    {
        private int _userId;
        private List<Notification> _notifications = new();
        private NotificationSettings _settings = new();

        public NotificationsPage(int userId)
        {
            InitializeComponent();
            _userId = userId;
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
                // � �������� ���������� ����� ����� HTTP-������
                _notifications = new List<Notification>
                {
                    new() { Id = 1, UserId = _userId, Title = "����� ����", Description = "�������", Time = new TimeSpan(8, 30, 0), Type = NotificationType.Meal },
                    new() { Id = 2, UserId = _userId, Title = "����������", Description = "������� ����������", Time = new TimeSpan(12, 0, 0), Type = NotificationType.Workout },
                    new() { Id = 3, UserId = _userId, Title = "����", Description = "������ ����", Time = new TimeSpan(15, 0, 0), Type = NotificationType.Water }
                };

                UpdateNotificationsUI();
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
                // � �������� ���������� ����� ����� HTTP-������
                _settings = new NotificationSettings
                {
                    MotivationalEnabled = true,
                    AchievementsEnabled = true,
                    ProgressEnabled = true,
                    GeneralEnabled = true,
                    SoundEnabled = true,
                    VibrationEnabled = true
                };

                // ��������� Switch ��������
                MotivationalSwitch.IsToggled = _settings.MotivationalEnabled;
                AchievementsSwitch.IsToggled = _settings.AchievementsEnabled;
                ProgressSwitch.IsToggled = _settings.ProgressEnabled;
                GeneralSwitch.IsToggled = _settings.GeneralEnabled;
                SoundSwitch.IsToggled = _settings.SoundEnabled;
                VibrationSwitch.IsToggled = _settings.VibrationEnabled;
            }
            catch (Exception ex)
            {
                await DisplayAlert("������", $"�� ������� ��������� ���������: {ex.Message}", "OK");
            }
        }

        private void UpdateNotificationsUI()
        {
            NotificationsContainer.Children.Clear();

            foreach (var notification in _notifications)
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
                timeFrame.Content = new Label
                {
                    Text = notification.Time.ToString(@"hh\:mm"),
                    FontSize = 18,
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center
                };

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
                switchControl.Toggled += async (s, e) => await OnNotificationToggled(notification.Id, e.Value);

                grid.Add(timeFrame, 0, 0);
                grid.Add(contentStack, 1, 0);
                grid.Add(switchControl, 2, 0);

                NotificationsContainer.Children.Add(grid);
            }
        }

        private async Task OnNotificationToggled(int notificationId, bool isActive)
        {
            var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                notification.IsActive = isActive;
                // ����� ����� ���������� ������� �� �������
            }
        }

        private async void OnAddNotificationClicked(object sender, EventArgs e)
        {
            await DisplayAlert("����������", "������� ���������� ����������� ����� ����������� �����", "OK");
        }

        private async void OnSelectTimeClicked(object sender, EventArgs e)
        {
            var result = await DisplayPromptAsync("����� �������", "������� ����� � ������� HH:mm", "OK", "������", keyboard: Keyboard.Numeric);
            if (!string.IsNullOrWhiteSpace(result))
            {
                if (TimeSpan.TryParse(result, out var time))
                {
                    await DisplayAlert("�������", $"������� �����: {time:hh\\:mm}", "OK");
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

            // ����� ����� ���������� �������� �� �������
        }
    }
}
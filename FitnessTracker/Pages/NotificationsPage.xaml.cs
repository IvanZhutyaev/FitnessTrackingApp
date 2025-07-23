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
    // Добавляем модели прямо в этом файле
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
                "Ты сегодня уже на шаг ближе к своей цели!",
                "Маленькие шаги приводят к большим результатам!",
                "Помни, почему ты начал - это поможет не сдаваться!"
            },
            ["Achievements"] = new List<string>
            {
                "Поздравляем! Вы достигли новой цели!",
                "Отличная работа! Вы выполнили недельный план!",
                "Новый рекорд! Вы превзошли себя!"
            },
            ["Progress"] = new List<string>
            {
                "За неделю вы прошли на 15% больше!",
                "Ваш прогресс впечатляет - продолжайте в том же духе!",
                "Анализ показал, что вы стали выносливее на 20%"
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
                    await DisplayAlert("Ошибка", "Не удалось загрузить напоминания", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить напоминания: {ex.Message}", "OK");
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
                    await DisplayAlert("Ошибка", "Не удалось загрузить настройки", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить настройки: {ex.Message}", "OK");
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
                "Изменить время",
                "Введите новое время в формате HH:mm",
                "Сохранить",
                "Отмена",
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
                        await Toast.Make("Время напоминания изменено", ToastDuration.Short).Show();
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", "Не удалось сохранить изменения", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("Ошибка", "Некорректный формат времени", "OK");
                }
            }
        }

        private async void OnAddNotificationClicked(object sender, EventArgs e)
        {
            var result = await DisplayActionSheet(
                "Добавить напоминание",
                "Отмена",
                null,
                "Прием пищи", "Тренировка", "Вода", "Другое");

            if (result != "Отмена" && result != null)
            {
                var timeResult = await DisplayPromptAsync(
                    "Укажите время",
                    "Введите время в формате HH:mm",
                    "Добавить",
                    "Отмена",
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
                        await Toast.Make("Напоминание добавлено", ToastDuration.Short).Show();
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", "Не удалось добавить напоминание", "OK");
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
                await DisplayAlert("Ошибка", "Не удалось обновить статус напоминания", "OK");
                notification.IsActive = !isActive; // Откатываем изменение
                UpdateNotificationsUI();
            }
        }

        private async void OnViewMessagesClicked(object sender, EventArgs e)
        {
            var category = await DisplayActionSheet(
                "Выберите тип сообщений",
                "Отмена",
                null,
                "Мотивационные", "Достижения", "Прогресс");

            if (category != null && category != "Отмена")
            {
                var key = category switch
                {
                    "Мотивационные" => "Motivational",
                    "Достижения" => "Achievements",
                    "Прогресс" => "Progress",
                    _ => ""
                };

                if (!string.IsNullOrEmpty(key) && _motivationalMessages.ContainsKey(key))
                {
                    var message = string.Join("\n\n", _motivationalMessages[key]);
                    await DisplayAlert($"Примеры {category.ToLower()} сообщений", message, "Понятно");
                }
            }
        }

        private async void OnSelectTimeClicked(object sender, EventArgs e)
        {
            var result = await DisplayPromptAsync(
                "Выбор времени уведомлений",
                "Введите время в формате HH:mm",
                "Сохранить",
                "Отмена",
                keyboard: Keyboard.Numeric);

            if (!string.IsNullOrWhiteSpace(result))
            {
                if (TimeSpan.TryParse(result, out var time))
                {
                    _settings.NotificationTime = time;
                    await SaveSettings();
                    await Toast.Make($"Время уведомлений установлено на {time:hh\\:mm}", ToastDuration.Short).Show();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Некорректный формат времени", "OK");
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
                    await DisplayAlert("Ошибка", "Не удалось сохранить настройки", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось сохранить настройки: {ex.Message}", "OK");
            }
        }

        private string GetDefaultDescription(string title)
        {
            return title switch
            {
                "Прием пищи" => "Основной прием пищи",
                "Тренировка" => "Запланированная тренировка",
                "Вода" => "Стакан воды",
                _ => "Персонализированное напоминание"
            };
        }

        private NotificationType GetNotificationType(string title)
        {
            return title switch
            {
                "Прием пищи" => NotificationType.Meal,
                "Тренировка" => NotificationType.Workout,
                "Вода" => NotificationType.Water,
                _ => NotificationType.Custom
            };
        }
    }
}
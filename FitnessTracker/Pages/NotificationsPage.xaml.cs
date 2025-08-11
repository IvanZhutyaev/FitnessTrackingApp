using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using Plugin.LocalNotification.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FitnessTrackingApp.Pages
{
    public partial class NotificationsPage : ContentPage
    {
        private readonly HttpClient _httpClient = new();
        private const string ApiBaseUrl = "http://83.166.244.89:5024";
        private int _userId;
        private List<Notification> _notifications = new();

        private const int MotivationalNotificationId = 1000;
        private const int AchievementNotificationId = 1001;
        private const int ProgressNotificationId = 1002;
        private const int TestNotificationId = 999;

        private bool _isTestNotificationInProgress = false;

        public NotificationsPage(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _httpClient.BaseAddress = new Uri(ApiBaseUrl);

            LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationTapped;
            LoadNotificationSettings();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Запрос разрешений для iOS
            if (DeviceInfo.Platform == DevicePlatform.iOS)
            {
                var granted = await LocalNotificationCenter.Current.RequestNotificationPermission();
                if (!granted)
                {
                    await DisplayAlert("Внимание", "Разрешения на уведомления не предоставлены. Пожалуйста, включите их в настройках.", "OK");
                }
            }

            LoadData();
        }

        private void LoadNotificationSettings()
        {
            MotivationalSwitch.IsToggled = Preferences.Get("Notification_Motivational", true);
            AchievementsSwitch.IsToggled = Preferences.Get("Notification_Achievements", true);
            ProgressSwitch.IsToggled = Preferences.Get("Notification_Progress", true);
            GeneralSwitch.IsToggled = Preferences.Get("Notification_General", true);
            SoundSwitch.IsToggled = Preferences.Get("Notification_Sound", true);
            VibrationSwitch.IsToggled = Preferences.Get("Notification_Vibration", true);
        }

        private async void LoadData()
        {
            await LoadNotifications();
            UpdateSystemNotifications();
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
                    await DisplayAlert("Ошибка", $"Ошибка загрузки уведомлений: {response.StatusCode}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка подключения: {ex.Message}", "OK");
            }
        }

        private void UpdateSystemNotifications()
        {
            CancelNotification(MotivationalNotificationId);
            CancelNotification(AchievementNotificationId);
            CancelNotification(ProgressNotificationId);

            if (MotivationalSwitch.IsToggled)
                ScheduleMotivationalNotification();

            if (AchievementsSwitch.IsToggled)
                ScheduleAchievementNotification();

            if (ProgressSwitch.IsToggled)
                ScheduleProgressNotification();
        }

        private void OnNotificationTapped(NotificationActionEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (e.Request.NotificationId == MotivationalNotificationId)
                {
                    await DisplayAlert("Мотивация", "Вы сегодня просто великолепны!", "OK");
                }
                await Shell.Current.GoToAsync("//notifications");
            });
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
                    HorizontalOptions = LayoutOptions.End,
                    BindingContext = notification
                };
                switchControl.Toggled += OnNotificationToggled;

                var deleteButton = new ImageButton
                {
                    Source = "delete.png",
                    BackgroundColor = Colors.Transparent,
                    HeightRequest = 30,
                    WidthRequest = 30,
                    HorizontalOptions = LayoutOptions.End,
                    VerticalOptions = LayoutOptions.Center,
                    BindingContext = notification
                };
                deleteButton.Clicked += OnDeleteNotificationClicked;

                grid.Add(timeFrame, 0, 0);
                grid.Add(contentStack, 1, 0);
                grid.Add(switchControl, 2, 0);
                grid.Add(deleteButton, 3, 0);

                NotificationsContainer.Children.Add(grid);
            }
        }

        private async Task ChangeNotificationTime(Notification notification, Label timeLabel)
        {
            var result = await DisplayPromptAsync("Изменить время", "Введите новое время в формате HH:mm", "Сохранить", "Отмена",
                                                  initialValue: notification.Time.ToString(@"hh\:mm"), keyboard: Keyboard.Numeric);

            if (!string.IsNullOrWhiteSpace(result) && TimeSpan.TryParse(result, out var newTime))
            {
                notification.Time = newTime;
                timeLabel.Text = newTime.ToString(@"hh\:mm");

                var response = await _httpClient.PutAsJsonAsync($"/notifications/{notification.Id}", notification);
                if (response.IsSuccessStatusCode)
                {
                    if (notification.IsActive)
                    {
                        CancelNotification(notification.Id);
                        ScheduleNotification(notification);
                    }
                    await Toast.Make("Время изменено", ToastDuration.Short).Show();
                }
            }
        }

        private async void OnDeleteNotificationClicked(object sender, EventArgs e)
        {
            var button = (ImageButton)sender;
            var notification = (Notification)button.BindingContext;

            bool confirm = await DisplayAlert("Удаление", $"Удалить напоминание '{notification.Title}'?", "Удалить", "Отмена");

            if (confirm)
            {
                var response = await _httpClient.DeleteAsync($"/notifications/{notification.Id}");
                if (response.IsSuccessStatusCode)
                {
                    _notifications.Remove(notification);
                    CancelNotification(notification.Id);
                    UpdateNotificationsUI();
                    await Toast.Make("Напоминание удалено", ToastDuration.Short).Show();
                }
            }
        }

        private async void OnNotificationToggled(object sender, ToggledEventArgs e)
        {
            var switchControl = (Switch)sender;
            var notification = (Notification)switchControl.BindingContext;

            notification.IsActive = e.Value;

            var response = await _httpClient.PutAsJsonAsync($"/notifications/{notification.Id}", notification);
            if (response.IsSuccessStatusCode)
            {
                if (e.Value)
                    ScheduleNotification(notification);
                else
                    CancelNotification(notification.Id);
            }
            else
            {
                switchControl.IsToggled = !e.Value;
            }
        }

        private void ScheduleNotification(Notification notification)
        {
            CancelNotification(notification.Id);

            var notifyTime = DateTime.Today.Add(notification.Time);
            if (notifyTime < DateTime.Now)
                notifyTime = notifyTime.AddDays(1);

            var request = new NotificationRequest
            {
                NotificationId = notification.Id,
                Title = notification.Title,
                Description = notification.Description,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = notifyTime,
                    RepeatType = NotificationRepeat.Daily
                }
            };

            // Настройки для Android
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                request.Android = new AndroidOptions
                {
                    ChannelId = "general_notifications",
                    AutoCancel = true,
                    VibrationPattern = VibrationSwitch.IsToggled ? new long[] { 100, 200, 300 } : null
                };
            }
            // Настройки для iOS
            else if (DeviceInfo.Platform == DevicePlatform.iOS)
            {
                // Для iOS не требуется дополнительных настроек в текущей версии плагина
            }

            LocalNotificationCenter.Current.Show(request);
        }

        private void ScheduleMotivationalNotification()
        {
            var notifyTime = DateTime.Today.AddHours(12);
            if (notifyTime < DateTime.Now)
                notifyTime = notifyTime.AddDays(1);

            var request = new NotificationRequest
            {
                NotificationId = MotivationalNotificationId,
                Title = "💪 Мотивация дня",
                Description = GetRandomMotivationalMessage(),
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = notifyTime,
                    RepeatType = NotificationRepeat.Daily
                }
            };

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                request.Android = new AndroidOptions
                {
                    ChannelId = "general_notifications",
                    AutoCancel = true,
                    VibrationPattern = VibrationSwitch.IsToggled ? new long[] { 100, 200, 300 } : null
                };
            }

            LocalNotificationCenter.Current.Show(request);
        }

        private void ScheduleAchievementNotification()
        {
            var notifyTime = DateTime.Today.AddHours(20);
            if (notifyTime < DateTime.Now)
                notifyTime = notifyTime.AddDays(1);

            var request = new NotificationRequest
            {
                NotificationId = AchievementNotificationId,
                Title = "🏆 Проверка достижений",
                Description = "Пора проверить ваши спортивные достижения!",
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = notifyTime,
                    RepeatType = NotificationRepeat.Daily
                }
            };

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                request.Android = new AndroidOptions
                {
                    ChannelId = "general_notifications",
                    AutoCancel = true,
                    VibrationPattern = VibrationSwitch.IsToggled ? new long[] { 100, 200, 300 } : null
                };
            }

            LocalNotificationCenter.Current.Show(request);
        }

        private void ScheduleProgressNotification()
        {
            var notifyTime = DateTime.Today.AddHours(21);
            if (notifyTime < DateTime.Now)
                notifyTime = notifyTime.AddDays(1);

            var request = new NotificationRequest
            {
                NotificationId = ProgressNotificationId,
                Title = "📊 Ваш прогресс",
                Description = "Не забудьте внести данные о вашей активности сегодня",
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = notifyTime,
                    RepeatType = NotificationRepeat.Daily
                }
            };

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                request.Android = new AndroidOptions
                {
                    ChannelId = "general_notifications",
                    AutoCancel = true,
                    VibrationPattern = VibrationSwitch.IsToggled ? new long[] { 100, 200, 300 } : null
                };
            }

            LocalNotificationCenter.Current.Show(request);
        }

        private string GetRandomMotivationalMessage()
        {
            var messages = new List<string>
            {
                "Сегодня тот день, когда вы станете лучше!",
                "Ваш прогресс впечатляет! Продолжайте в том же духе!",
                "Каждая тренировка делает вас сильнее!",
                "Помните, почему вы начали! Вы ближе к цели, чем думаете!",
                "Успех — это сумма небольших усилий, повторяемых изо дня в день!"
            };

            return messages[new Random().Next(messages.Count)];
        }

        private void CancelNotification(int notificationId)
        {
            LocalNotificationCenter.Current.Cancel(notificationId);
        }

        private async void OnAddNotificationClicked(object sender, EventArgs e)
        {
            var result = await DisplayActionSheet("Добавить напоминание", "Отмена", null,
                "Прием пищи", "Тренировка", "Вода", "Другое");

            if (result != "Отмена")
            {
                var timeResult = await DisplayPromptAsync("Введите время", "Формат HH:mm", "OK", "Отмена", keyboard: Keyboard.Numeric);

                if (!string.IsNullOrWhiteSpace(timeResult) && TimeSpan.TryParse(timeResult, out var time))
                {
                    var newNotification = new Notification
                    {
                        UserId = _userId,
                        Title = result,
                        Description = GetDescription(result),
                        Time = time,
                        IsActive = true,
                        Type = result switch
                        {
                            "Прием пищи" => NotificationType.Meal,
                            "Тренировка" => NotificationType.Workout,
                            "Вода" => NotificationType.Water,
                            _ => NotificationType.Custom
                        }
                    };

                    try
                    {
                        var response = await _httpClient.PostAsJsonAsync("/notifications", newNotification);
                        if (response.IsSuccessStatusCode)
                        {
                            var createdNotification = await response.Content.ReadFromJsonAsync<Notification>();
                            _notifications.Add(createdNotification);
                            UpdateNotificationsUI();
                            ScheduleNotification(createdNotification);
                            await Toast.Make("Напоминание добавлено", ToastDuration.Short).Show();
                        }
                        else
                        {
                            await DisplayAlert("Ошибка", "Не удалось создать уведомление", "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Ошибка", ex.Message, "OK");
                    }
                }
            }
        }

        private string GetDescription(string title) => title switch
        {
            "Прием пищи" => "Не забудьте поесть",
            "Тренировка" => "Время тренировки",
            "Вода" => "Выпейте стакан воды",
            _ => "Персонализированное напоминание"
        };

        private void OnSettingToggled(object sender, ToggledEventArgs e)
        {
            if (sender is not Switch switchControl)
                return;

            string settingType = switchControl.StyleId;
            bool isEnabled = e.Value;

            Preferences.Set($"Notification_{settingType}", isEnabled);

            switch (settingType)
            {
                case "Motivational":
                    if (isEnabled) ScheduleMotivationalNotification();
                    else CancelNotification(MotivationalNotificationId);
                    break;
                case "Achievements":
                    if (isEnabled) ScheduleAchievementNotification();
                    else CancelNotification(AchievementNotificationId);
                    break;
                case "Progress":
                    if (isEnabled) ScheduleProgressNotification();
                    else CancelNotification(ProgressNotificationId);
                    break;
                case "General":
                    if (!isEnabled)
                    {
                        MotivationalSwitch.IsToggled = false;
                        AchievementsSwitch.IsToggled = false;
                        ProgressSwitch.IsToggled = false;
                    }
                    break;
                case "Sound":
                case "Vibration":
                    UpdateAllActiveNotifications();
                    break;
            }

            Toast.Make($"Настройка '{GetSettingName(settingType)}' {(isEnabled ? "включена" : "выключена")}", ToastDuration.Short).Show();
        }

        private void UpdateAllActiveNotifications()
        {
            foreach (var notification in _notifications.Where(n => n.IsActive))
            {
                CancelNotification(notification.Id);
                ScheduleNotification(notification);
            }

            if (MotivationalSwitch.IsToggled)
            {
                CancelNotification(MotivationalNotificationId);
                ScheduleMotivationalNotification();
            }

            if (AchievementsSwitch.IsToggled)
            {
                CancelNotification(AchievementNotificationId);
                ScheduleAchievementNotification();
            }

            if (ProgressSwitch.IsToggled)
            {
                CancelNotification(ProgressNotificationId);
                ScheduleProgressNotification();
            }
        }

        private string GetSettingName(string settingType) => settingType switch
        {
            "Motivational" => "Мотивационные уведомления",
            "Achievements" => "Уведомления о достижениях",
            "Progress" => "Напоминания о прогрессе",
            "General" => "Общие уведомления",
            "Sound" => "Звуковые оповещения",
            "Vibration" => "Виброоповещения",
            _ => "Неизвестная настройка"
        };

        private async void OnViewMessagesClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Примеры сообщений",
                "💪 Вы уже на 75% пути к вашей цели!\n\n" +
                "🏋️ Сегодня отличный день для силовой тренировки!\n\n" +
                "🥗 Не забудьте про правильное питание после тренировки\n\n" +
                "🌟 Вы делаете потрясающие успехи! Так держать!",
                "Закрыть");
        }

        private async void OnTestNotificationClicked(object sender, EventArgs e)
        {
            if (_isTestNotificationInProgress)
                return;

            try
            {
                _isTestNotificationInProgress = true;

                CancelNotification(TestNotificationId);

                var request = new NotificationRequest
                {
                    NotificationId = TestNotificationId,
                    Title = "Тестовое уведомление",
                    Description = "Проверка работы системы уведомлений"
                };

                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    request.Android = new AndroidOptions
                    {
                        ChannelId = "test_notifications",
                        AutoCancel = true,
                        VibrationPattern = VibrationSwitch.IsToggled ? new long[] { 100, 200, 300 } : null
                    };
                }

                LocalNotificationCenter.Current.Show(request);
                await Toast.Make("Тестовое уведомление отправлено", ToastDuration.Short).Show();
            }
            finally
            {
                _isTestNotificationInProgress = false;
            }
        }
    }

    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TimeSpan Time { get; set; }
        public bool IsActive { get; set; } = true;
        public NotificationType Type { get; set; } = NotificationType.Custom;
    }

    public enum NotificationType
    {
        Meal,
        Workout,
        Water,
        Custom
    }
}

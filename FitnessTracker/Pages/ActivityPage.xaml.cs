using System.Net.Http.Json;
using FitnessTrackingApp.Models; // Добавлено для использования общей модели
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitnessTrackingApp.Pages;

public partial class ActivityPage : ContentPage
{
    private readonly IStepsService _stepService;
    private readonly HttpClient _httpClient = new HttpClient();
    private const string ApiBaseUrl = "http://localhost:5024";

    // Константы для расчетов
    private const double StepLength = 0.7; // Длина шага в метрах
    private const double CaloriesPerStep = 0.04; // Калорий на шаг

    // Текущие данные активности
    private int _currentSteps;
    private double _currentDistance;
    private int _currentCalories;

    // Таймеры
    private IDispatcherTimer _updateUiTimer;
    private IDispatcherTimer _saveToDbTimer;

    // Недельный прогресс
    private const int WeeklyGoal = 70000; // Недельная цель по шагам

    public ActivityPage()
    {
        if (UserSession.UserId == 0)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Ошибка", "Сначала войдите в аккаунт", "OK");
                await Navigation.PopAsync();
            });
            return;
        }

        InitializeComponent();
        _stepService = ServiceHelper.GetService<IStepsService>();

        // Инициализация таймеров
        SetupTimers();

        // Первоначальная загрузка данных
        LoadActivityData();
        LoadWeeklyProgress();
    }

    private void SetupTimers()
    {
        // Таймер обновления UI (каждые 5 секунд)
        _updateUiTimer = Dispatcher.CreateTimer();
        _updateUiTimer.Interval = TimeSpan.FromSeconds(5);
        _updateUiTimer.Tick += (s, e) => UpdateActivityData();
        _updateUiTimer.Start();

        // Таймер сохранения в БД (каждый час)
        _saveToDbTimer = Dispatcher.CreateTimer();
        _saveToDbTimer.Interval = TimeSpan.FromHours(1);
        _saveToDbTimer.Tick += async (s, e) => await SaveActivityToDatabase();
        _saveToDbTimer.Start();
    }

    private void LoadActivityData()
    {
        _currentSteps = _stepService.GetSteps();
        CalculateDerivedMetrics();
        UpdateUi();
    }

    private void UpdateActivityData()
    {
        _currentSteps = _stepService.GetSteps();
        CalculateDerivedMetrics();
        UpdateUi();
    }

    private void CalculateDerivedMetrics()
    {
        // Расчет дистанции в км
        _currentDistance = Math.Round((_currentSteps * StepLength) / 1000, 1);

        // Расчет калорий
        _currentCalories = (int)(_currentSteps * CaloriesPerStep);
    }

    private void UpdateUi()
    {
        StepsLabel.Text = _currentSteps.ToString();
        DistanceLabel.Text = $"{_currentDistance} км";
        CaloriesLabel.Text = _currentCalories.ToString();
    }

    private async void LoadWeeklyProgress()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/activities/stats/{UserSession.UserId}");
            if (response.IsSuccessStatusCode)
            {
                var stats = await response.Content.ReadFromJsonAsync<List<ActivityStat>>();
                if (stats != null && stats.Any())
                {
                    var totalSteps = stats.Sum(s => s.Steps);
                    var progress = (double)totalSteps / WeeklyGoal;

                    WeeklyProgressBar.Progress = progress;
                    WeeklyProgressLabel.Text = $"{Math.Round(progress * 100)}% от цели";
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки прогресса: {ex.Message}");
        }
    }

    private async Task SaveActivityToDatabase()
    {
        try
        {
            var activity = new Models.Activity // Явное указание пространства имен
            {
                Steps = _currentSteps,
                Distance = _currentDistance,
                Calories = _currentCalories,
                UserId = UserSession.UserId,
                Date = DateTime.UtcNow
            };

            var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/user/steps", activity);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Ошибка сохранения активности");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка сохранения: {ex.Message}");
        }
    }

    private void OnPeriodButtonClicked(object sender, EventArgs e)
    {
        // Сбрасываем цвет всех кнопок
        DayButton.BackgroundColor = Color.FromArgb("#2A4D80");
        DayButton.TextColor = Color.FromArgb("#A0E7FF");
        WeekButton.BackgroundColor = Color.FromArgb("#2A4D80");
        WeekButton.TextColor = Color.FromArgb("#A0E7FF");

        // Устанавливаем цвет активной кнопки
        var button = (Button)sender;
        button.BackgroundColor = Color.FromArgb("#00C9FF");
        button.TextColor = Color.FromArgb("#0C1B33");

        // Загружаем данные для выбранного периода
        if (button == DayButton)
        {
            LoadDayData();
        }
        else
        {
            LoadWeekData();
        }
    }

    private void LoadDayData()
    {
        // Реализация загрузки данных за день
        Console.WriteLine("Загрузка данных за день");
    }

    private void LoadWeekData()
    {
        // Реализация загрузки данных за неделю
        Console.WriteLine("Загрузка данных за неделю");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _updateUiTimer.Stop();
        _saveToDbTimer.Stop();
    }
}

public class ActivityStat
{
    public DateTime Date { get; set; }
    public int Steps { get; set; }
    public double Distance { get; set; }
    public int Calories { get; set; }
}
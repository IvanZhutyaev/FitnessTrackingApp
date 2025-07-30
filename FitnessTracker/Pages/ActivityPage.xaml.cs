using FitnessTrackingApp.Models; // Добавлено для использования общей модели
using FitnessTrackingApp.Models;
using FitnessTrackingApp.Services;
using FitnessTrackingApp.Services;
using FitnessTrackingApp.Services;
using Microcharts;
using Microcharts.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls;
using SkiaSharp;
using Syncfusion.Maui.Charts;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq;
using System.Net.Http.Json;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Syncfusion.Maui.Charts;
using Microsoft.Maui.Graphics;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;
using Syncfusion.Maui.Charts;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;
namespace FitnessTrackingApp.Pages;

public partial class ActivityPage : ContentPage
{
    private readonly IStepsService _stepService;
    private readonly HttpClient _httpClient = new HttpClient();
    private const string ApiBaseUrl = "http://localhost:5024";
    private bool _isDayView = true;
    private SfCartesianChart _chart;

    // Константы для расчетов
    private const double StepLength = 0.7;
    private const double CaloriesPerStep = 0.04;

    // Текущие данные активности
    private int _currentSteps;
    private double _currentDistance;
    private int _currentCalories;

    // Таймеры
    private IDispatcherTimer _updateUiTimer;
    private IDispatcherTimer _saveToDbTimer;

    // Недельный прогресс
    private const int WeeklyGoal = 70000;

    public ActivityPage(IStepsService stepService)
    {
        _stepService = stepService;
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
        InitializeChart();
        _stepService = ServiceHelper.GetService<IStepsService>();

        SetupTimers();
        LoadActivityData();
        LoadWeeklyProgress();
        LoadData();
    }
    private void InitializeChart()
    {
        _chart = new SfCartesianChart
        {
            Background = Colors.Transparent,
            Margin = new Thickness(0, 10, 0, 0),
            HeightRequest = 200
        };

        var xAxis = new DateTimeAxis
        {
            LabelStyle = new ChartAxisLabelStyle { TextColor = Colors.White },
            MajorGridLineStyle = new ChartLineStyle { Stroke = Colors.Gray.WithAlpha(0.3f) }
        };

        var yAxis = new NumericalAxis
        {
            LabelStyle = new ChartAxisLabelStyle { TextColor = Colors.White },
            MajorGridLineStyle = new ChartLineStyle { Stroke = Colors.Gray.WithAlpha(0.3f) }
        };

        _chart.XAxes.Add(xAxis);
        _chart.YAxes.Add(yAxis);

        var chartFrame = (Frame)FindByName("ChartFrame");
        chartFrame.Content = _chart;
    }
    private void LoadData()
    {
        StepsLabel.Text = _stepService.GetSteps().ToString();
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _stepService.Start();
        await UpdateChartData();
    }



    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _stepService.Stop();
        _updateUiTimer.Stop();
        _saveToDbTimer.Stop();
    }



    private async Task UpdateChartData()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/activities/{UserSession.UserId}");
            if (response.IsSuccessStatusCode)
            {
                var activities = await response.Content.ReadFromJsonAsync<List<Activity>>();
                if (activities != null && activities.Any())
                {
                    var filteredActivities = _isDayView
                        ? activities.Where(a => a.Date.Date == DateTime.Today).OrderBy(a => a.Date)
                        : activities.Where(a => a.Date >= DateTime.Today.AddDays(-7)).OrderBy(a => a.Date);

                    var series = new LineSeries()
                    {
                        ItemsSource = filteredActivities,
                        XBindingPath = "Date",
                        YBindingPath = "Steps",
                        ShowMarkers = true,
                        PaletteBrushes = new List<Brush> { new SolidColorBrush(Colors.Cyan) }
                    };

                    // Создаем стиль для линии
                    var style = new ChartLineStyle()
                    {
                        Stroke = new SolidColorBrush(Colors.Cyan),
                    };

                    
                    
                    series.Fill = new SolidColorBrush(Colors.Cyan.WithAlpha(0.2f));

                    _chart.Series.Clear();
                    _chart.Series.Add(series);

                    // Настраиваем оси
                    if (_isDayView)
                    {
                        ((DateTimeAxis)_chart.XAxes[0]).Minimum = DateTime.Today;
                        ((DateTimeAxis)_chart.XAxes[0]).Maximum = DateTime.Today.AddDays(1);
                    }
                    else
                    {
                        ((DateTimeAxis)_chart.XAxes[0]).Minimum = DateTime.Today.AddDays(-7);
                        ((DateTimeAxis)_chart.XAxes[0]).Maximum = DateTime.Today.AddDays(1);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки данных для графика: {ex.Message}");
        }
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

    private async void LoadDayData()
    {
        _isDayView = true;
        await UpdateChartData();
    }

    private async void LoadWeekData()
    {
        _isDayView = false;
        await UpdateChartData();
    }


}

public class ActivityStat
{
    public DateTime Date { get; set; }
    public int Steps { get; set; }
    public double Distance { get; set; }
    public int Calories { get; set; }
}
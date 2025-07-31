using FitnessTrackingApp.Models;
using FitnessTrackingApp.Services;
using Microcharts.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FitnessTrackingApp.Pages;

public class ActivityChartDrawable : IDrawable
{
    public List<Activity> DayActivities { get; set; } = new();
    public List<Activity> WeekActivities { get; set; } = new();
    public bool IsDayView { get; set; } = true;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var activities = IsDayView ? DayActivities : WeekActivities;
        if (!activities.Any()) return;

        var textColor = Colors.White;
        var lineColor = Colors.Cyan;
        var fillColor = new Color(0, 201, 255, 50);
        var pointColor = Colors.Cyan;
        var axisColor = Colors.Gray.WithAlpha(0.5f);

        var maxValue = activities.Max(a => a.Steps) * 1.1f;
        if (maxValue <= 0) maxValue = 1000;
        var minValue = 0;
        var valueRange = maxValue - minValue;

        // Оси
        canvas.StrokeColor = axisColor;
        canvas.StrokeSize = 1;
        canvas.DrawLine(40, dirtyRect.Height - 30, dirtyRect.Width, dirtyRect.Height - 30); // X
        canvas.DrawLine(40, 0, 40, dirtyRect.Height - 30); // Y

        // Подписи осей
        canvas.FontColor = textColor;
        canvas.FontSize = 10;

        // Точки графика
        var points = new List<PointF>();
        for (int i = 0; i < activities.Count; i++)
        {
            var x = 40 + (i * (dirtyRect.Width - 40) / Math.Max(1, activities.Count - 1));
            var y = dirtyRect.Height - 30 - ((activities[i].Steps - minValue) / valueRange * (dirtyRect.Height - 40));
            points.Add(new PointF(x, y));
        }

        // Заливка под графиком
        if (points.Count > 1)
        {
            var path = new PathF();
            path.MoveTo(40, dirtyRect.Height - 30);
            foreach (var point in points)
                path.LineTo(point);
            path.LineTo(points.Last().X, dirtyRect.Height - 30);
            path.Close();

            canvas.FillColor = fillColor;
            canvas.FillPath(path);
        }

        // Линия графика
        if (points.Count > 1)
        {
            canvas.StrokeColor = lineColor;
            canvas.StrokeSize = 2;
            var path = new PathF();
            path.MoveTo(points[0]);
            for (int i = 1; i < points.Count; i++)
                path.LineTo(points[i]);
            canvas.DrawPath(path);
        }

        // Точки
        foreach (var point in points)
        {
            canvas.FillColor = pointColor;
            canvas.FillCircle(point, 4);
        }

        // Подписи X
        for (int i = 0; i < activities.Count; i++)
        {
            var label = IsDayView
                ? activities[i].Date.ToString("HH:mm")
                : activities[i].Date.ToString("dd.MM");
            var x = 40 + (i * (dirtyRect.Width - 40) / Math.Max(1, activities.Count - 1));
            canvas.DrawString(label, x - 20, dirtyRect.Height - 20, 40, 20,
                HorizontalAlignment.Center, VerticalAlignment.Top);
        }
    }
}

public partial class ActivityPage : ContentPage
{
    private readonly IStepsService _stepService;
    private readonly HttpClient _httpClient = new();
    private const string ApiBaseUrl = "http://localhost:5024";
    private bool _isDayView = true;
    private readonly ActivityChartDrawable _chartDrawable = new();
    private IDispatcherTimer _chartUpdateTimer;
    private IDispatcherTimer _updateUiTimer;
    private IDispatcherTimer _saveToDbTimer;

    private int _currentSteps;
    private double _currentDistance;
    private int _currentCalories;
    private const double StepLength = 0.7;
    private const double CaloriesPerStep = 0.04;
    private const int WeeklyGoal = 70000;

    public ActivityPage(IStepsService stepService)
    {
        InitializeComponent();

        _stepService = stepService ?? throw new ArgumentNullException(nameof(stepService));
        _chartDrawable = new ActivityChartDrawable();

        if (UserSession.UserId == 0)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Ошибка", "Сначала войдите в аккаунт", "OK");
                await Navigation.PopAsync();
            });
            return;
        }

        InitializeChart();
        SetupChartUpdateTimer();
        SetupTimers();
        LoadActivityData();
        LoadWeeklyProgress();
        LoadData();
    }

    private void InitializeChart()
    {
        if (ChartGraphicsView != null)
        {
            ChartGraphicsView.Drawable = _chartDrawable;
        }
        else
        {
            Console.WriteLine("ChartGraphicsView is null during initialization");
        }
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler != null && ChartGraphicsView.Drawable == null)
        {
            ChartGraphicsView.Drawable = _chartDrawable;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _stepService.Start();
        await UpdateChartData();
        ChartGraphicsView.Invalidate(); // <-- ВАЖНО
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _stepService.Stop();
        _updateUiTimer?.Stop();
        _saveToDbTimer?.Stop();
    }

    private void SetupChartUpdateTimer()
    {
        _chartUpdateTimer = Dispatcher.CreateTimer();
        _chartUpdateTimer.Interval = TimeSpan.FromMinutes(5);
        _chartUpdateTimer.Tick += async (s, e) =>
        {
            await UpdateChartData();
            ChartGraphicsView.Invalidate();
        };
        _chartUpdateTimer.Start();
    }

    private void SetupTimers()
    {
        _updateUiTimer = Dispatcher.CreateTimer();
        _updateUiTimer.Interval = TimeSpan.FromSeconds(5);
        _updateUiTimer.Tick += (s, e) => UpdateActivityData();
        _updateUiTimer.Start();

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
        _currentDistance = Math.Round((_currentSteps * StepLength) / 1000, 1);
        _currentCalories = (int)(_currentSteps * CaloriesPerStep);
    }

    private void UpdateUi()
    {
        StepsLabel.Text = _currentSteps.ToString();
        DistanceLabel.Text = $"{_currentDistance} км";
        CaloriesLabel.Text = _currentCalories.ToString();
    }

    private void LoadData()
    {
        StepsLabel.Text = _stepService.GetSteps().ToString();
    }

    private async Task UpdateChartData()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/activities/stats/{UserSession.UserId}");
            if (response.IsSuccessStatusCode)
            {
                var stats = await response.Content.ReadFromJsonAsync<List<ActivityStat>>();
                if (stats != null && stats.Any())
                {
                    var today = DateTime.Today;

                    _chartDrawable.DayActivities = stats
                        .Where(s => s.Date.Date == today)
                        .Select(s => new Activity { Date = s.Date, Steps = s.Steps })
                        .ToList();

                    _chartDrawable.WeekActivities = stats
                        .Where(s => s.Date >= today.AddDays(-7))
                        .GroupBy(s => s.Date.Date)
                        .Select(g => new Activity
                        {
                            Date = g.Key,
                            Steps = g.Sum(x => x.Steps)
                        })
                        .OrderBy(a => a.Date)
                        .ToList();

                    _chartDrawable.IsDayView = _isDayView;
                    ChartGraphicsView.Invalidate();
                }
                else
                {
                    Console.WriteLine("Нет данных для отображения графика");
                }
            }
            else
            {
                Console.WriteLine("Ошибка при получении статистики: " + response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обновления графика: {ex.Message}");
        }
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
            var activity = new Models.Activity
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
        DayButton.BackgroundColor = Color.FromArgb("#2A4D80");
        DayButton.TextColor = Color.FromArgb("#A0E7FF");
        WeekButton.BackgroundColor = Color.FromArgb("#2A4D80");
        WeekButton.TextColor = Color.FromArgb("#A0E7FF");

        var button = (Button)sender;
        button.BackgroundColor = Color.FromArgb("#00C9FF");
        button.TextColor = Color.FromArgb("#0C1B33");

        if (button == DayButton)
            LoadDayData();
        else
            LoadWeekData();
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

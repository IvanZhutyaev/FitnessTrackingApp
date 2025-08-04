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

        canvas.StrokeColor = axisColor;
        canvas.StrokeSize = 1;
        canvas.DrawLine(40, dirtyRect.Height - 30, dirtyRect.Width, dirtyRect.Height - 30);
        canvas.DrawLine(40, 0, 40, dirtyRect.Height - 30);

        canvas.FontColor = textColor;
        canvas.FontSize = 10;

        var points = new List<PointF>();
        for (int i = 0; i < activities.Count; i++)
        {
            var x = 40 + (i * (dirtyRect.Width - 40) / Math.Max(1, activities.Count - 1));
            var y = dirtyRect.Height - 30 - ((activities[i].Steps - minValue) / valueRange * (dirtyRect.Height - 40));
            points.Add(new PointF(x, y));
        }

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

        foreach (var point in points)
        {
            canvas.FillColor = pointColor;
            canvas.FillCircle(point, 4);
        }

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
    private ActivityChartDrawable _chartDrawable = new();
    private IDispatcherTimer _chartUpdateTimer;
    private IDispatcherTimer _updateUiTimer;
    private IDispatcherTimer _saveToDbTimer;
    private double _currentDistance;
    private int _currentCalories;
    private const double StepLength = 0.7;
    private const double CaloriesPerStep = 0.04;
    private const int WeeklyGoal = 70000;
    private bool _isInitialLoad = true;
    private const int ChartUpdateIntervalSeconds = 30;
    private int summarySteps = UserStaticData.Steps;
    private int stepsOnTick = 0;
    private int _todaySteps = 0;

    public ActivityPage(IStepsService stepService)
    {
        InitializeComponent();
        _stepService = stepService;
        _stepService = stepService ?? throw new ArgumentNullException(nameof(stepService));
        _chartDrawable = new ActivityChartDrawable();

        InitializeChart();
        SetupChartUpdateTimer();
        SetupTimers();
        LoadActivityData();
        LoadWeeklyProgress();
    }

    private void InitializeChart()
    {
        if (ChartGraphicsView != null)
        {
            _chartDrawable = new ActivityChartDrawable
            {
                DayActivities = new List<Activity>(),
                WeekActivities = new List<Activity>(),
                IsDayView = true
            };

            ChartGraphicsView.Drawable = _chartDrawable;
            ChartGraphicsView.Invalidate();
        }
    }
    private void SetupMidnightResetTimer()
    {
        var now = DateTime.Now;
        var midnight = now.Date.AddDays(1);
        var timeUntilMidnight = midnight - now;

        var midnightTimer = new System.Timers.Timer(timeUntilMidnight.TotalMilliseconds);
        midnightTimer.Elapsed += async (s, e) =>
        {
            midnightTimer.Stop();

            await SaveActivityToDatabase();
            stepsOnTick = 0;
            CalculateDerivedMetrics();
            UpdateUi();
            ChartGraphicsView.Invalidate();

            SetupMidnightResetTimer();
        };
        midnightTimer.AutoReset = false;
        midnightTimer.Start();
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

        if (_isInitialLoad)
        {
            await LoadInitialData();
            _isInitialLoad = false;
        }

        _stepService.Start();
        StartAllTimers();
    }
    private async Task LoadInitialData()
    {
        await LoadSavedSteps();
        await UpdateAllData();
    }
    private async Task UpdateAllData()
    {
        await UpdateChartData();
        LoadWeeklyProgress();
        ChartGraphicsView.Invalidate();
    }
    private void StartAllTimers()
    {
        _updateUiTimer = Dispatcher.CreateTimer();
        _updateUiTimer.Interval = TimeSpan.FromSeconds(5);
        _updateUiTimer.Tick += (s, e) => UpdateActivityData();
        _updateUiTimer.Start();

        _chartUpdateTimer = Dispatcher.CreateTimer();
        _chartUpdateTimer.Interval = TimeSpan.FromSeconds(ChartUpdateIntervalSeconds);
        _chartUpdateTimer.Tick += async (s, e) => await UpdateAllData();
        _chartUpdateTimer.Start();

        _saveToDbTimer = Dispatcher.CreateTimer();
        _saveToDbTimer.Interval = TimeSpan.FromMinutes(1);
        _saveToDbTimer.Tick += async (s, e) => await SaveActivityToDatabase();
        _saveToDbTimer.Start();
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _stepService.Stop();

        Task.Run(async () =>
        {
            await SaveActivityToDatabase();
        }).Wait();

        StopAllTimers();
    }


    private void StopAllTimers()
    {
        _updateUiTimer?.Stop();
        _chartUpdateTimer?.Stop();
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
    private async Task LoadSavedSteps()
    {
        try
        {
            var today = DateTime.Today;
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/activities/stats/{UserSession.Username}");

            if (response.IsSuccessStatusCode)
            {
                var stats = await response.Content.ReadFromJsonAsync<List<ActivityStat>>();
                if (stats != null && stats.Any())
                {
                    var todaySteps = stats
                        .Where(a => a.Date.Date == today)
                        .Sum(a => a.Steps);

                    _todaySteps = todaySteps;
                    CalculateDerivedMetrics();
                    UpdateUi();
                }
            }
            else
            {
                Console.WriteLine("Ошибка запроса шагов из БД: " + response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки шагов из БД: {ex.Message}");
        }
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
        stepsOnTick = _stepService.GetSteps();
        CalculateDerivedMetrics();
        UpdateUi();
    }

    private void UpdateActivityData()
    {
        stepsOnTick = _stepService.GetSteps();

        CalculateDerivedMetrics();
        UpdateUi();
    }

    private void CalculateDerivedMetrics()
    {
        _currentDistance = Math.Round((UserStaticData.Steps * StepLength) / 1000, 1);
        _currentCalories = (int)(UserStaticData.Steps * CaloriesPerStep);
    }

    private void UpdateUi()
    {
        StepsLabel.Text = _todaySteps.ToString();
        DistanceLabel.Text = $"{_currentDistance} км";
        CaloriesLabel.Text = _currentCalories.ToString();

    }


    private async Task UpdateChartData()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/activities/stats/{UserSession.Username}");
            if (response.IsSuccessStatusCode)
            {
                var stats = await response.Content.ReadFromJsonAsync<List<ActivityStat>>();
                if (stats != null)
                {
                    var today = DateTime.Today;

                    _chartDrawable.DayActivities = stats
                        .Where(s => s.Date.Date == today)
                        .GroupBy(s => s.Date.Hour)
                        .Select(g => new Activity
                        {
                            Date = today.AddHours(g.Key),
                            Steps = g.Sum(x => x.Steps)
                        })
                        .OrderBy(a => a.Date)
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

                    ChartGraphicsView.Invalidate();
                }
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
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/activities/stats/{UserSession.Username}");
            if (response.IsSuccessStatusCode)
            {
                var stats = await response.Content.ReadFromJsonAsync<List<ActivityStat>>();
                if (stats != null && stats.Any())
                {
                    var totalSteps = stats.Sum(s => s.Steps);
                    var progress = Math.Min(1.0, (double)totalSteps / WeeklyGoal);

                    //UserStaticData.Steps = totalSteps; //Костально но пока можно

                    WeeklyProgressBar.Progress = progress;
                    WeeklyProgressPercentLabel.Text = $"{Math.Round(progress * 100)}%";
                    WeeklyProgressLabel.Text = $"{totalSteps:N0} шагов из {WeeklyGoal:N0}";
                }
                else
                {
                    WeeklyProgressBar.Progress = 0;
                    WeeklyProgressPercentLabel.Text = "0%";
                    WeeklyProgressLabel.Text = $"0 шагов из {WeeklyGoal:N0}";
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
            var activity = new
            {
                Username = UserSession.Username,
                Date = DateTime.UtcNow,
                Steps = stepsOnTick,
                Distance = _currentDistance,
                Calories = _currentCalories
            };

            var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/user/steps", activity);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Ошибка сохранения: " + await response.Content.ReadAsStringAsync());
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
        _chartDrawable.IsDayView = true;
        await UpdateChartData();
        ChartGraphicsView.Invalidate();
    }

    private async void LoadWeekData()
    {
        _isDayView = false;
        _chartDrawable.IsDayView = false;
        await UpdateChartData();
        ChartGraphicsView.Invalidate();
    }
}

public class ActivityStat
{
    public DateTime Date { get; set; }
    public int Steps { get; set; }
    public double Distance { get; set; }
    public int Calories { get; set; }
}

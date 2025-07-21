using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System;
using System.Net.Http.Headers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using FitnessTrackingApp.Models;
namespace FitnessTrackingApp.Pages;
using Microsoft.Maui.Media;
using System.Reflection;
using System.Text.Json.Serialization;
public partial class WorkoutsPage : ContentPage, INotifyPropertyChanged
{
    private readonly HttpClient _httpClient = new HttpClient();
    private const string ApiBaseUrl = "http://localhost:5024";
    public ObservableCollection<Exercise> Exercises { get; set; } = new();
    private ObservableCollection<Exercise> _allExercises = new();
    private Exercise _currentExercise;
    private IDispatcherTimer _timer;
    private int _remainingSeconds;
    public ObservableCollection<WorkoutHistory> WorkoutHistoryList { get; } = new();
    private WorkoutDetailDto _currentWorkoutDetails;
    private string _searchText;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                FilterExercises();
            }
        }
    }

    public WorkoutsPage()
    {
        InitializeComponent();
        BindingContext = this;
        _timer = Application.Current.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTimerTick;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadExercisesAsync();
        await LoadWorkoutHistory(); // Добавьте эту строку
    }
    private async void OnStartWorkoutClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Exercise exercise)
        {
            _currentExercise = exercise;
            TimerWorkoutName.Text = exercise.Name;
            _remainingSeconds = exercise.Duration * 60;
            UpdateTimerDisplay();
            await ShowTimerPopup();
            _timer.Start();
        }
    }

    private async void OnCompleteWorkoutClicked(object sender, EventArgs e)
    {
        _timer.Stop();
        await CompleteWorkout();
        await HideTimerPopup();
    }
    private void UpdateTimerDisplay()
    {
        var ts = TimeSpan.FromSeconds(_remainingSeconds);
        TimerDisplay.Text = $"{ts.Minutes:00}:{ts.Seconds:00}";
    }
    private async void OnTimerTick(object sender, EventArgs e)
    {
        _remainingSeconds--;
        UpdateTimerDisplay();

        if (_remainingSeconds <= 0)
        {
            _timer.Stop();
            PlayCompletionSound();
            await CompleteWorkout();
        }
    }
    private async Task ShowTimerPopup()
    {
        TimerPopup.IsVisible = true;
        await TimerPopup.FadeTo(1, 200);
    }

    private async Task HideTimerPopup()
    {
        await TimerPopup.FadeTo(0, 200);
        TimerPopup.IsVisible = false;
    }
    private async void PlayCompletionSound()
    {
        try
        {
            // Вибрация
            if (Vibration.Default.IsSupported)
                Vibration.Default.Vibrate(500);

            // Звук через системный API
            await TextToSpeech.SpeakAsync("Тренировка завершена", new SpeechOptions()
            {
                Pitch = 1.5f,
                Volume = 1.0f
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка звука: {ex.Message}");
        }
    }
    private async Task CompleteWorkout()
    {
        if (_currentExercise == null) return;

        try
        {
            // 1. Создаем запись для истории
            var historyItem = new
            {
                UserId = _currentExercise.UserId,
                WorkoutName = _currentExercise.Name,
                Duration = _currentExercise.Duration, // в минутах
                CaloriesBurned = _currentExercise.CaloriesBurned,
                Date = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(historyItem);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 2. Отправляем запрос на добавление в историю
            var addResponse = await _httpClient.PostAsync($"{ApiBaseUrl}/workouthistory", content);

            if (addResponse.IsSuccessStatusCode)
            {
                // 3. Удаляем из текущих упражнений
                var deleteResponse = await _httpClient.DeleteAsync($"{ApiBaseUrl}/exercises/{_currentExercise.Id}");

                if (deleteResponse.IsSuccessStatusCode)
                {
                    // 4. Обновляем оба списка
                    await LoadExercisesAsync();
                    await LoadWorkoutHistory();
                    _currentExercise = null;
                }
            }
            else
            {
                await DisplayAlert("Ошибка", "Не удалось добавить в историю", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }

    private async Task LoadWorkoutHistory()
    {
        try
        {
            var userId = UserSession.UserId;
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/workouthistory/{userId}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var history = JsonSerializer.Deserialize<List<WorkoutHistory>>(json);

                WorkoutHistoryList.Clear();
                if (history != null)
                {
                    foreach (var item in history)
                    {
                        WorkoutHistoryList.Add(item);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось загрузить историю: {ex.Message}", "OK");
        }
    }


    private async Task LoadExercisesAsync()
    {
        try
        {
            var userId = UserSession.UserId;
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/exercises/{userId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var exercises = JsonSerializer.Deserialize<List<Exercise>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                _allExercises.Clear();
                Exercises.Clear();
                if (exercises != null)
                {
                    foreach (var ex in exercises)
                    {
                        _allExercises.Add(ex);
                        Exercises.Add(ex);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }

    private void FilterExercises()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Exercises.Clear();
            foreach (var ex in _allExercises)
                Exercises.Add(ex);
        }
        else
        {
            var filtered = _allExercises.Where(ex =>
                ex.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

            Exercises.Clear();
            foreach (var ex in filtered)
                Exercises.Add(ex);
        }
    }

    private void OnAddWorkoutClicked(object sender, EventArgs e)
    {
        AddWorkoutPopup.IsVisible = true;
    }

    private void OnCancelAddWorkoutClicked(object sender, EventArgs e)
    {
        AddWorkoutPopup.IsVisible = false;
        WorkoutNameEntry.Text = string.Empty;
        WorkoutDescriptionEntry.Text = string.Empty;
        WorkoutDurationEntry.Text = string.Empty;
        WorkoutCaloriesEntry.Text = string.Empty;
    }

    private async void OnSaveWorkoutClicked(object sender, EventArgs e)
    {
        var name = WorkoutNameEntry.Text?.Trim();
        var description = WorkoutDescriptionEntry.Text?.Trim();
        var durationStr = WorkoutDurationEntry.Text?.Trim();
        var caloriesStr = WorkoutCaloriesEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlert("Ошибка", "Введите название тренировки", "OK");
            return;
        }

        if (!int.TryParse(durationStr, out int duration))
        {
            await DisplayAlert("Ошибка", "Введите корректную длительность (сек)", "OK");
            return;
        }

        if (!int.TryParse(caloriesStr, out int calories))
        {
            await DisplayAlert("Ошибка", "Введите корректное количество калорий", "OK");
            return;
        }

        var exercise = new Exercise
        {
            UserId = UserSession.UserId,
            Name = name,
            Description = description,
            Duration = duration,
            CaloriesBurned = calories,
            Date = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(exercise);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync($"{ApiBaseUrl}/exercises", content);
            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Успех", "Упражнение добавлено", "OK");
                AddWorkoutPopup.IsVisible = false;
                WorkoutNameEntry.Text = string.Empty;
                WorkoutDescriptionEntry.Text = string.Empty;
                WorkoutDurationEntry.Text = string.Empty;
                WorkoutCaloriesEntry.Text = string.Empty;
                await LoadExercisesAsync();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Ошибка", $"Ошибка при добавлении: {error}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }
    private async void OnDetailsClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is WorkoutHistory workout)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}/workouthistory/details/{workout.Id}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var details = JsonSerializer.Deserialize<WorkoutDetailDto>(json);

                    DetailWorkoutName.Text = details.WorkoutName;
                    DetailDate.Text = details.Date.ToString("dd MMMM yyyy HH:mm");
                    DetailDuration.Text = $"{details.Duration} минут";
                    DetailCalories.Text = $"{details.CaloriesBurned} ккал";
                    DetailNotes.Text = details.Notes;
                    DetailDescription.Text = details.Description; // Добавьте этот Label в XAML

                    await ShowDetailsPopup();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }
    }

    private async Task ShowDetailsPopup()
    {
        DetailsPopup.IsVisible = true;
        await DetailsPopup.FadeTo(1, 200);
    }

    private async void OnCloseDetailsClicked(object sender, EventArgs e)
    {
        await HideDetailsPopup();
    }

    private async Task HideDetailsPopup()
    {
        await DetailsPopup.FadeTo(0, 200);
        DetailsPopup.IsVisible = false;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

	public class Exercise
	{
		public int Id { get; set; }
		public int UserId { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public int Duration { get; set; }
		public int CaloriesBurned { get; set; }
		public DateTime Date { get; set; }
	}

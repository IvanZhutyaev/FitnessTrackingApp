using FitnessTrackingApp.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FitnessTrackingApp.Pages
{
    public partial class WorkoutsPage : ContentPage, INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "http://localhost:5024";
        private string _searchText;
        private Exercise _currentExercise;
        private IDispatcherTimer _timer;
        private int _remainingSeconds;

        public ObservableCollection<Exercise> Exercises { get; } = new ObservableCollection<Exercise>();
        public ObservableCollection<WorkoutHistory> WorkoutHistoryList { get; } = new ObservableCollection<WorkoutHistory>();
        private ObservableCollection<Exercise> _allExercises = new ObservableCollection<Exercise>();

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
            await LoadWorkoutHistory();
        }

        private async Task LoadWorkoutHistory()
        {
            try
            {
                var userId = UserSession.UserId;
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}/workouthistory/{userId}");

                if (response.IsSuccessStatusCode)
                {
                    var history = await response.Content.ReadFromJsonAsync<List<WorkoutHistory>>();

                    WorkoutHistoryList.Clear();
                    foreach (var item in history)
                    {
                        WorkoutHistoryList.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
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
                    var exercises = await response.Content.ReadFromJsonAsync<List<Exercise>>();

                    _allExercises.Clear();
                    Exercises.Clear();

                    foreach (var ex in exercises)
                    {
                        _allExercises.Add(ex);
                        Exercises.Add(ex);
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
                await CompleteWorkout();
                await HideTimerPopup();
            }
        }

        private async Task CompleteWorkout()
        {
            if (_currentExercise == null) return;

            try
            {
                var historyItem = new WorkoutHistory
                {
                    UserId = _currentExercise.UserId,
                    WorkoutName = _currentExercise.Name,
                    Description = _currentExercise.Description,
                    Duration = _currentExercise.Duration,
                    CaloriesBurned = _currentExercise.CaloriesBurned,
                    Date = DateTime.UtcNow,
                    Notes = "Завершено через таймер"
                };

                var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/workouthistory", historyItem);

                if (response.IsSuccessStatusCode)
                {
                    await _httpClient.DeleteAsync($"{ApiBaseUrl}/exercises/{_currentExercise.Id}");
                    await LoadExercisesAsync();
                    await LoadWorkoutHistory();
                    _currentExercise = null;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
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

        private async void OnCompleteWorkoutClicked(object sender, EventArgs e)
        {
            _timer.Stop();
            await CompleteWorkout();
            await HideTimerPopup();
        }

        private async void OnSaveWorkoutClicked(object sender, EventArgs e)
        {
            var exercise = new Exercise
            {
                UserId = UserSession.UserId,
                Name = WorkoutNameEntry.Text,
                Description = WorkoutDescriptionEntry.Text,
                Duration = int.TryParse(WorkoutDurationEntry.Text, out var duration) ? duration : 0,
                CaloriesBurned = int.TryParse(WorkoutCaloriesEntry.Text, out var calories) ? calories : 0,
                Date = DateTime.UtcNow
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/exercises", exercise);
                if (response.IsSuccessStatusCode)
                {
                    AddWorkoutPopup.IsVisible = false;
                    await LoadExercisesAsync();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private void OnAddWorkoutClicked(object sender, EventArgs e)
        {
            AddWorkoutPopup.IsVisible = true;
        }

        private void OnCancelAddWorkoutClicked(object sender, EventArgs e)
        {
            AddWorkoutPopup.IsVisible = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
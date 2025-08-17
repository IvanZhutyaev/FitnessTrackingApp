﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using FitnessTrackingApp.Models;
using System.Net.Http.Json;

namespace FitnessTrackingApp.Pages;

public partial class NutritionPage : ContentPage, INotifyPropertyChanged
{
    private int _userId = UserSession.UserId;
    private NutritionDay _currentDay = new();
    public ObservableCollection<Meal> Meals { get; } = new();
    public event PropertyChangedEventHandler PropertyChanged;
    private readonly HttpClient _httpClient = new HttpClient();
    private DateTime? _lastLoadedDate;

    public double WaterIntake
    {
        get => _currentDay?.WaterIntake ?? 0;
        set
        {
            if (_currentDay != null)
            {
                _currentDay.WaterIntake = value;
                OnPropertyChanged(nameof(WaterIntake));
                OnPropertyChanged(nameof(WaterProgressValue));
                OnPropertyChanged(nameof(WaterDisplayText));
            }
        }
    }

    public double WaterGoal
    {
        get => _currentDay?.WaterGoal ?? 2.0;
        set
        {
            if (_currentDay != null)
            {
                _currentDay.WaterGoal = value;
                OnPropertyChanged(nameof(WaterGoal));
                OnPropertyChanged(nameof(WaterProgressValue));
                OnPropertyChanged(nameof(WaterDisplayText));
            }
        }
    }

    public double WaterProgressValue => WaterGoal > 0 ? WaterIntake / WaterGoal : 0;
    public string WaterDisplayText => $"{WaterIntake:F1}/{WaterGoal:F1} л";

    public int TotalCalories => _currentDay?.TotalCalories ?? 0;
    public double TotalProtein => _currentDay?.TotalProtein ?? 0;
    public double TotalFat => _currentDay?.TotalFat ?? 0;
    public double TotalCarbs => _currentDay?.TotalCarbs ?? 0;

    public NutritionPage()
    {
        InitializeComponent();

        if (UserSession.UserId == 0)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Ошибка", "Сначала войдите в аккаунт", "OK");
                await Navigation.PopAsync();
            });
            return;
        }

        BindingContext = this;
        LoadNutritionData();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_lastLoadedDate == null || _lastLoadedDate.Value.Date < DateTime.UtcNow.Date)
        {
            LoadNutritionData();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }

    protected void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private async void LoadNutritionData()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiUrl.ApiBaseUrl}/nutrition/{_userId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<NutritionResponse>(content, options)
                         ?? new NutritionResponse();

                Device.BeginInvokeOnMainThread(() =>
                {
                    _currentDay = data.NutritionDay ?? new NutritionDay
                    {
                        UserId = _userId,
                        Date = DateTime.UtcNow.Date,
                        WaterGoal = 2.0
                    };

                    Meals.Clear();
                    foreach (var meal in data.Meals ?? new List<Meal>())
                    {
                        Meals.Add(meal);
                    }

                    UpdateUI();
                    _lastLoadedDate = DateTime.UtcNow;
                });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }

    private void UpdateUI()
    {
        OnPropertyChanged(nameof(TotalCalories));
        OnPropertyChanged(nameof(TotalProtein));
        OnPropertyChanged(nameof(TotalFat));
        OnPropertyChanged(nameof(TotalCarbs));
        OnPropertyChanged(nameof(WaterIntake));
        OnPropertyChanged(nameof(WaterGoal));
        OnPropertyChanged(nameof(WaterProgressValue));
        OnPropertyChanged(nameof(WaterDisplayText));

        MealsCollectionView.ItemsSource = null;
        MealsCollectionView.ItemsSource = Meals;
    }

    private async void OnAddMealClicked(object sender, EventArgs e)
    {
        try
        {
            var mealType = await DisplayActionSheet("Тип приема пищи", "Отмена", null,
                "Завтрак", "Обед", "Ужин", "Перекус");

            if (mealType == "Отмена") return;

            var name = await DisplayPromptAsync("Название блюда", "Введите название:");
            if (string.IsNullOrWhiteSpace(name)) return;

            var calories = await DisplayPromptAsync("Калории", "Введите количество калорий:", keyboard: Keyboard.Numeric);
            var protein = await DisplayPromptAsync("Белки", "Введите количество белков (г):", keyboard: Keyboard.Numeric);
            var fat = await DisplayPromptAsync("Жиры", "Введите количество жиров (г):", keyboard: Keyboard.Numeric);
            var carbs = await DisplayPromptAsync("Углеводы", "Введите количество углеводов (г):", keyboard: Keyboard.Numeric);

            if (!int.TryParse(calories, out var caloriesValue) ||
                !double.TryParse(protein, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var proteinValue) ||
                !double.TryParse(fat, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var fatValue) ||
                !double.TryParse(carbs, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var carbsValue))

            {
                await DisplayAlert("Ошибка", "Некорректные числовые значения", "OK");
                return;
            }

            var meal = new Meal
            {
                UserId = _userId,
                MealType = mealType,
                Name = name,
                Calories = caloriesValue,
                Protein = proteinValue,
                Fat = fatValue,
                Carbs = carbsValue,
                Date = DateTime.UtcNow
            };

            await SaveMealAsync(meal);

            _currentDay.TotalCalories += meal.Calories;
            _currentDay.TotalProtein += meal.Protein;
            _currentDay.TotalFat += meal.Fat;
            _currentDay.TotalCarbs += meal.Carbs;
            Meals.Add(meal);
            UpdateUI();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }

    private async Task SaveMealAsync(Meal meal)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{ApiUrl.ApiBaseUrl}/meals", meal);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception(error);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка сохранения: {ex.Message}");
        }
    }

    private async void OnAddWaterClicked(object sender, EventArgs e)
    {
        try
        {
            var amount = await DisplayPromptAsync("Добавить воду",
                $"Сколько воды вы выпили? (Текущая цель: {WaterGoal:F1} л)",
                keyboard: Keyboard.Numeric,
                initialValue: "0.2");

            if (!string.IsNullOrWhiteSpace(amount) &&
                double.TryParse(amount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var waterAmount))
            {
                WaterIntake += waterAmount;
                await SaveWaterIntakeAsync(waterAmount);

                if (WaterIntake >= WaterGoal)
                {
                    await DisplayAlert("Поздравляем!", "Вы достигли суточной нормы воды!", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }

    private async Task SaveWaterIntakeAsync(double amount)
    {
        try
        {
            var request = new WaterUpdateRequest
            {
                UserId = _userId,
                Amount = amount
            };
            var response = await _httpClient.PostAsJsonAsync($"{ApiUrl.ApiBaseUrl}/water", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception(error);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка сохранения: {ex.Message}");
        }
    }

    private async void SavePendingChanges()
    {
        try
        {
            // Сохраняем текущее состояние воды
            if (_currentDay != null)
            {
                await SaveWaterIntakeAsync(WaterIntake);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при сохранении: {ex.Message}");
        }
    }
}

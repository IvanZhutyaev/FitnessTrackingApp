using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using FitnessTrackingApp.Models;

namespace FitnessTrackingApp.Pages;

public partial class NutritionPage : ContentPage, INotifyPropertyChanged
{
    private int _userId = UserSession.UserId;
    private NutritionDay _currentDay = new();
    public ObservableCollection<Meal> Meals { get; } = new();

    public event PropertyChangedEventHandler PropertyChanged;

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

    public double WaterProgressValue => WaterIntake / WaterGoal;
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
        LoadNutritionData();
    }

    protected void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private async void LoadNutritionData()
    {
        try
        {
            var client = new HttpClient();
            var response = await client.GetAsync($"http://localhost:5024/nutrition/{_userId}");

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
                !double.TryParse(protein, out var proteinValue) ||
                !double.TryParse(fat, out var fatValue) ||
                !double.TryParse(carbs, out var carbsValue))
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

            // Локальное обновление
            _currentDay.TotalCalories += meal.Calories;
            _currentDay.TotalProtein += meal.Protein;
            _currentDay.TotalFat += meal.Fat;
            _currentDay.TotalCarbs += meal.Carbs;
            Meals.Add(meal);
            UpdateUI();

            await SaveMealAsync(meal);
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
            var client = new HttpClient();
            var json = JsonSerializer.Serialize(meal);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await client.PostAsync("http://localhost:5024/meals", content);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
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

            if (!string.IsNullOrWhiteSpace(amount) && double.TryParse(amount, out var waterAmount))
            {
                // Локальное обновление
                WaterIntake += waterAmount;

                await SaveWaterIntakeAsync(WaterIntake);
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

            var client = new HttpClient();
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await client.PostAsync("http://localhost:5024/water", content);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }
}
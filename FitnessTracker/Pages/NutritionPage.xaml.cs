using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using FitnessTrackingApp.Models;

namespace FitnessTrackingApp.Pages;

public partial class NutritionPage : ContentPage
{
    private int _userId = UserSession.UserId;
    private NutritionDay _currentDay;
    private const string ApiBaseUrl = "http://localhost:5024";

    public ObservableCollection<Meal> Meals { get; } = new();

    public NutritionPage()
    {
        InitializeComponent();
        if (UserSession.UserId == 0)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("������", "������� ������� � �������", "OK");
                await Navigation.PopAsync();
            });
            return;
        }
        BindingContext = this;
        LoadNutritionData();
    }

    private async void LoadNutritionData()
    {
        try
        {
            var client = new HttpClient();
            var response = await client.GetAsync($"{ApiBaseUrl}/nutrition/{_userId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<NutritionResponse>(content);

                _currentDay = data?.NutritionDay ?? new NutritionDay
                {
                    UserId = _userId,
                    Date = DateTime.UtcNow.Date,
                    WaterGoal = 2.0
                };

                Meals.Clear();
                foreach (var meal in data?.Meals ?? new List<Meal>())
                {
                    Meals.Add(meal);
                }

                UpdateUI();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("������", ex.Message, "OK");
        }
    }

    private void UpdateUI()
    {
        if (_currentDay == null) return;

        CaloriesLabel.Text = $"{_currentDay.TotalCalories}/2400";
        ProteinLabel.Text = $"{_currentDay.TotalProtein:F0}/150�";
        FatLabel.Text = $"{_currentDay.TotalFat:F0}/80�";
        CarbsLabel.Text = $"{_currentDay.TotalCarbs:F0}/300�";
        WaterLabel.Text = $"{_currentDay.WaterIntake:F1}/{_currentDay.WaterGoal:F1} �";
        WaterProgress.Progress = _currentDay.WaterIntake / _currentDay.WaterGoal;
    }

    private async void OnAddMealClicked(object sender, EventArgs e)
    {
        var mealType = await DisplayActionSheet("��� ������ ����", "������", null,
            "�������", "����", "����", "�������");

        if (mealType == "������") return;

        var name = await DisplayPromptAsync("�������� �����", "������� ��������:");
        if (string.IsNullOrWhiteSpace(name)) return;

        var calories = await DisplayPromptAsync("�������", "������� ���������� �������:", keyboard: Keyboard.Numeric);
        var protein = await DisplayPromptAsync("�����", "������� ���������� ������ (�):", keyboard: Keyboard.Numeric);
        var fat = await DisplayPromptAsync("����", "������� ���������� ����� (�):", keyboard: Keyboard.Numeric);
        var carbs = await DisplayPromptAsync("��������", "������� ���������� ��������� (�):", keyboard: Keyboard.Numeric);

        if (!int.TryParse(calories, out var caloriesValue) ||
            !double.TryParse(protein, out var proteinValue) ||
            !double.TryParse(fat, out var fatValue) ||
            !double.TryParse(carbs, out var carbsValue))
        {
            await DisplayAlert("������", "������������ �������� ��������", "OK");
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
            Carbs = carbsValue
        };

        try
        {
            var client = new HttpClient();
            var json = JsonSerializer.Serialize(meal);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{ApiBaseUrl}/meals", content);

            if (response.IsSuccessStatusCode)
            {
                LoadNutritionData();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("������", ex.Message, "OK");
        }
    }

    private async void OnAddWaterClicked(object sender, EventArgs e)
    {
        var amount = await DisplayPromptAsync("�������� ����",
            $"������� ���� �� ������? (������� ����: {_currentDay?.WaterGoal ?? 2.0} �)",
            keyboard: Keyboard.Numeric,
            initialValue: "0.2");

        if (!string.IsNullOrWhiteSpace(amount) && double.TryParse(amount, out var waterAmount))
        {
            try
            {
                var request = new WaterUpdateRequest
                {
                    UserId = _userId,
                    Amount = waterAmount
                };

                var client = new HttpClient();
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{ApiBaseUrl}/water", content);

                if (response.IsSuccessStatusCode)
                {
                    LoadNutritionData();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("������", ex.Message, "OK");
            }
        }
    }
}
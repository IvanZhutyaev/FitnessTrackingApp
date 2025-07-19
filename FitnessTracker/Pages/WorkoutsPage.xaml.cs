using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System;

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace FitnessTrackingApp.Pages;

public partial class WorkoutsPage : ContentPage, INotifyPropertyChanged
{
	private readonly HttpClient _httpClient = new HttpClient();
	private const string ApiBaseUrl = "http://localhost:5024";
	public ObservableCollection<Exercise> Exercises { get; set; } = new();

	public WorkoutsPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadExercisesAsync();
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
				Exercises.Clear();
				if (exercises != null)
				{
					foreach (var ex in exercises)
						Exercises.Add(ex);
				}
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

	public event PropertyChangedEventHandler PropertyChanged;
	protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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
}
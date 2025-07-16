using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace FitnessTrackingApp.Pages;

public partial class ProfilePage : ContentPage
{
	// Пример: заменить на актуальные значения или привязки
	int userId = UserSession.UserId;
	private const string ApiBaseUrl = "http://localhost:5024";
	public ProfilePage()
	{
		InitializeComponent();
	}

	private async void OnSaveProfileChangesClicked(object sender, EventArgs e)
	{
		
		int age = ageEntry.Text != null ? int.Parse(ageEntry.Text) : 28;
		double height = heightEntry.Text != null ? double.Parse(heightEntry.Text) : 180;
		double weight = weightEntry.Text != null ? double.Parse(weightEntry.Text) : 75;

		var changeInfo = new {
			UserId = userId != 0 ? userId : 0,
			Age = age,
			Weight = weight,
			Height = height
		};

		var json = JsonSerializer.Serialize(changeInfo);
		var content = new StringContent(json, Encoding.UTF8, "application/json");

		using var client = new HttpClient();
		try
		{
			var response = await client.PostAsync($"{ApiBaseUrl}/user/changeinfo", content);
			if (response.IsSuccessStatusCode)
			{
				await DisplayAlert("Успех", "Данные успешно сохранены", "OK");
			}
			else
			{
				var error = await response.Content.ReadAsStringAsync();
				await DisplayAlert("Ошибка", $"Ошибка сохранения: {error}", "OK");
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert("Ошибка", ex.Message, "OK");
		}
	}
}
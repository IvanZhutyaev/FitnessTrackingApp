using System.Net.Http.Json;

namespace FitnessTrackingApp.Pages;

public partial class ActivityPage : ContentPage
{
	private readonly IStepsService _stepService;
	private readonly HttpClient _httpClient = new HttpClient();
    private const string ApiBaseUrl = "http://localhost:5024";
	private void UpdateSteps(string step)
	{
		StepsLabel.Text = step;
	}

	private async void StepsToDataBase(object sender, EventArgs e, int step)
	{
		try
		{
			var steps = new Activity
			{
				Steps = step,
				UserId = UserSession.UserId
			};

			var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/user/steps", steps);

			if (response.IsSuccessStatusCode)
			{
				await DisplayAlert("Успех", "Шаги записаны в БД", "OK");
			}
			else
			{
				await DisplayAlert("Ошибка", "Не удалось обновить шаги", "OK");
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"Failed to update steps: {ex.Message}", "OK");
		}



	}


	public ActivityPage()
	{
        if (UserSession.UserId == 0)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Ошибка", "Сначала войдите в аккаунт", "OK");
                await Navigation.PopAsync(); // Возврат на предыдущую страницу
            });
            return;
        }
        InitializeComponent();
		_stepService = ServiceHelper.GetService<IStepsService>();
		int steps = _stepService.GetSteps();
		//Обновление шагов на экране
		Dispatcher.StartTimer(TimeSpan.FromSeconds(5), () =>
		{
			UpdateSteps(steps.ToString());

			return true;
		});
		//обновление шагов в БД
		Dispatcher.StartTimer(TimeSpan.FromHours(1), () =>
		{
			StepsToDataBase(this, EventArgs.Empty, steps);
			return true;
		});
	}
}

public class Activity
{
	public int UserId {get; set; }
	public int Steps { get; set; }
	


}
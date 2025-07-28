namespace FitnessTrackingApp.Pages;

public partial class ActivityPage : ContentPage
{
	private readonly IStepsService _stepService;

	private void UpdateSteps()
	{
		StepsLabel.Text = _stepService.GetSteps().ToString();
	}
	public ActivityPage()
	{
		InitializeComponent();
		_stepService = ServiceHelper.GetService<IStepsService>();

		Dispatcher.StartTimer(TimeSpan.FromSeconds(1), () =>
		{
			UpdateSteps();
			return true;
		});
	}
}
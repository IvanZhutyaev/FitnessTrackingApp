namespace FitnessTrackingApp.Pages; using System.Timers;

public partial class ProgressPage : ContentPage
{
    private Timer _timer;
    private bool isPageActive;
    private string _username = UserSession.Username;
    private int _userId = UserSession.UserId;

    private readonly HttpClient _httpClient = new HttpClient();
    private const string ApiBaseUrl = "http://127.0.0.1:5024";

    public ProgressPage()
    {
        InitializeComponent();
        _timer = new Timer(1000);
        _timer.Elapsed += OnTimerTick;

    }


    protected override void OnAppearing()
    {
        base.OnAppearing();
        isPageActive = true;
        _timer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        isPageActive = false;
        _timer.Stop();
    }

    private async void OnTimerTick(object sender, ElapsedEventArgs e)
    {
        try
        {
            if (!isPageActive)
            {
                return;
            }

            MainThread.BeginInvokeOnMainThread(async () =>
            {


                await LoadData();



            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task LoadData()
    {
        AvgStepsLabel.Text = UserStaticData.AvgSteps.ToString();
        AvgDistanceLabel.Text = $"{UserStaticData.AvgDistance.ToString()} км";
        //return Task.CompleteTask;
    }


}



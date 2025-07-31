using FitnessTrackingApp.Services;

namespace FitnessTrackingApp.Platforms.iOS;

public class iOSStepsService : IStepsService
{
    private int _steps = 0;
    private IDispatcherTimer? _timer;

    public void Start()
    {
        _timer = Application.Current.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) => _steps += Random.Shared.Next(1, 4);
        _timer.Start();
    }

    public void Stop()
    {
        _timer?.Stop();
    }

    public int GetSteps() => _steps;
}

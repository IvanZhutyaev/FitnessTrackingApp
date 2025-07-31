// Services/IStepsService.cs
namespace FitnessTrackingApp.Services;

public interface IStepsService
{
    void Start();
    void Stop();
    int GetSteps();
}

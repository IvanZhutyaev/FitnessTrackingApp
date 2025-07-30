// Services/IStepsService.cs
namespace FitnessTrackingApp.Services;

public interface IStepsService
{
    int GetSteps();
    void Start();
    void Stop();
}
// Services/DummyStepService.cs
namespace FitnessTrackingApp.Services;

public class DummyStepService : IStepsService
{
    private int _steps = 0;

    public int GetSteps() => _steps;

    public void Start() => _steps += 100; // Для тестов

    public void Stop() { }
}
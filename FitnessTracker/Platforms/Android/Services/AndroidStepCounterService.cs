using Android.Hardware;
using Android.Content;
using FitnessTrackingApp.Services;

namespace FitnessTrackingApp.Platforms.Android.Services;

public class AndroidStepCounterService : Java.Lang.Object, ISensorEventListener, IStepsService
{
    private readonly SensorManager _sensorManager;
    private readonly Sensor _stepCounterSensor;
    private int _totalSteps;

    public AndroidStepCounterService()
    {
        var context = global::Android.App.Application.Context;
        _sensorManager = (SensorManager)context.GetSystemService(Context.SensorService)!;
        _stepCounterSensor = _sensorManager.GetDefaultSensor(SensorType.StepCounter);
    }

    public int GetSteps() => _totalSteps;

    public void Start()
    {
        if (_stepCounterSensor != null)
            _sensorManager.RegisterListener(this, _stepCounterSensor, SensorDelay.Normal);
    }

    public void Stop() => _sensorManager.UnregisterListener(this);

    public void OnSensorChanged(SensorEvent e)
    {
        if (e.Sensor.Type == SensorType.StepCounter)
            _totalSteps = (int)e.Values[0];
    }

    public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy) { }
}

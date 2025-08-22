using Android.Content;
using Android.Hardware;
using FitnessTrackingApp.Services;

namespace FitnessTrackingApp.Platforms.Android;

public class AndroidStepsService : Java.Lang.Object, IStepsService, ISensorEventListener
{
    private SensorManager? _sensorManager;
    private Sensor? _stepSensor;
    private int _initialStepCount = -1;
    private int _steps = 0;

    public void Start()
    {
        _sensorManager = (SensorManager?)global::Android.App.Application.Context.GetSystemService(Context.SensorService)!;
        _stepSensor = _sensorManager?.GetDefaultSensor(SensorType.StepCounter);

        if (_stepSensor != null)
            _sensorManager?.RegisterListener(this, _stepSensor, SensorDelay.Ui);
    }

    public void Stop()
    {
        _sensorManager?.UnregisterListener(this);
    }

    public int GetSteps() => _steps;

    public void OnSensorChanged(SensorEvent? e)
    {
        if (e?.Sensor?.Type == SensorType.StepCounter)
        {
            int totalSteps = (int)e.Values[0];

            if (_initialStepCount < 0)
                _initialStepCount = totalSteps;

            _steps = totalSteps - _initialStepCount;
        }
    }

    public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy) { }
}

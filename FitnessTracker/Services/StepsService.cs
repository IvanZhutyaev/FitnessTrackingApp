using System.Numerics;
using Microsoft.Maui.Devices.Sensors;

namespace FitnessTrackingApp.Services
{
    public class StepsService : IStepsService
    {

        //Убрать после полной реализации
        #if DEBUG
        private System.Timers.Timer? _debugTimer;
        #endif

        private int _steps = 0;
        private Vector3 _lastAcceleration;
        private bool _isFirst = true;
        private const double Threshold = 1.2; // Можно подобрать опытным путём

        public StepsService()
        {
            Accelerometer.ReadingChanged += OnReadingChanged;
        }

        public void Start()
        {
            _steps = 0;
            _isFirst = true;
            try
            {
                Accelerometer.Start(SensorSpeed.UI);
            }
            catch
            { 
                //#if DEBUG
                    // Для отладки, если устройство не поддерживает акселерометр
                   // _debugTimer = new System.Timers.Timer(1000);
                    //_debugTimer.Elapsed += (s, e) => _steps++;
                    //_debugTimer.Start();
                //#endif
            }
        }

        public void Stop()
        {
            Accelerometer.Stop();
        }

        public int GetSteps() => _steps;

        private void OnReadingChanged(object? sender, AccelerometerChangedEventArgs e)
        {
            var acc = e.Reading.Acceleration;
            var vector = new Vector3((float)acc.X, (float)acc.Y, (float)acc.Z);

            if (_isFirst)
            {
                _lastAcceleration = vector;
                _isFirst = false;
                return;
            }

            var delta = Vector3.Distance(_lastAcceleration, vector);

            if (delta > Threshold)
                _steps++;

            _lastAcceleration = vector;
        }
    }
}
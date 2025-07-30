using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace FitnessTrackingApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override async void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Запрашиваем разрешение
            if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.ActivityRecognition)
                != Android.Content.PM.Permission.Granted)
            {
                ActivityCompat.RequestPermissions(
                    this,
                    new[] { Android.Manifest.Permission.ActivityRecognition },
                    100
                );
            }
        }
    }
}

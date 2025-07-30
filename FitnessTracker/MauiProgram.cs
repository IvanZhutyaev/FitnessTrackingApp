using CommunityToolkit.Maui;
using FitnessTrackingApp.Services;
using Plugin.LocalNotification;
using FitnessTrackingApp.Services;
using FitnessTrackingApp.Services;
using Syncfusion.Maui.Core.Hosting;
using Syncfusion.Licensing;
using FitnessTrackingApp.Pages;
#if ANDROID
using FitnessTrackingApp.Platforms.Android.Services;
#endif
namespace FitnessTrackingApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseLocalNotification()
                .ConfigureSyncfusionCore()
                
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .Services.AddSingleton<IStepsService, FitnessTrackingApp.Services.StepsService>()

                ;

#if ANDROID
            builder.Services.AddSingleton<IStepsService, AndroidStepCounterService>();
#else
builder.Services.AddSingleton<IStepsService, DummyStepService>();
#endif

            builder.Services.AddTransient<ActivityPage>();
            SyncfusionLicenseProvider.RegisterLicense("ваш_ключ_лицензии");
            var app = builder.Build();

            ServiceHelper.Services = app.Services;


            return app;
        }
    }
}

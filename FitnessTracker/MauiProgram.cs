using CommunityToolkit.Maui;
using FitnessTrackingApp.Pages;
using FitnessTrackingApp.Services;
using FitnessTrackingApp.Services;
using FitnessTrackingApp.Services;
using Microcharts.Maui;
using Plugin.LocalNotification;
using Syncfusion.Licensing;
using Syncfusion.Maui.Core.Hosting;
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
                .UseMicrocharts()
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

            var app = builder.Build();

            ServiceHelper.Services = app.Services;


            return app;
        }
    }
}

using CommunityToolkit.Maui;
using FitnessTrackingApp.Pages;
using FitnessTrackingApp.Services;
using Microcharts.Maui;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Syncfusion.Licensing;
using Syncfusion.Maui.Core.Hosting;
#if ANDROID
using FitnessTrackingApp.Platforms.Android;
#elif IOS
using FitnessTrackingApp.Platforms.iOS;
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
            
            builder.Services.AddSingleton<IStepsService, AndroidStepsService>();
#elif IOS
            builder.Services.AddSingleton<IStepsService, iOSStepsService>();
#else
        builder.Services.AddSingleton<IStepsService, FakeStepsService>(); // Windows / Mac
#endif

            builder.Services.AddTransient<ActivityPage>();

            var app = builder.Build();

            ServiceHelper.Services = app.Services;


            return app;
        }
    }
}

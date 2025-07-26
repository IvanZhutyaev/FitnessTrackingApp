using Microsoft.Extensions.DependencyInjection;

public static class ServiceHelper
{
    public static T GetService<T>() => Current.GetService<T>();

    public static IServiceProvider Current =>
        MauiApplication.Current.Services;
}

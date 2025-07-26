using Microsoft.Extensions.DependencyInjection;
using System;

public static class ServiceHelper
{
    public static IServiceProvider? Services { get; set; }

    public static T GetService<T>() where T : class
        => Services?.GetService<T>() ?? throw new InvalidOperationException("ServiceProvider is not initialized");
}
// App.xaml.cs
using FitnessTrackingApp.Services;
using FitnessTrackingApp.Pages;
using Microsoft.Maui.Controls;

namespace FitnessTrackingApp;

public partial class App : Application
{
    public App(IStepsService stepsService)
    {
        InitializeComponent();
        MainPage = new NavigationPage(new MainPage());
    }
}
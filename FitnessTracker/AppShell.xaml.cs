using FitnessTrackingApp.Pages; // Добавьте эту строку в начало файла
namespace FitnessTrackingApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("MainPage", typeof(MainPage));
            Routing.RegisterRoute("ProfilePage", typeof(ProfilePage));
            Routing.RegisterRoute("WorkoutsPage", typeof(WorkoutsPage));
            Routing.RegisterRoute("AdditionallyPage", typeof(AdditionallyPage));
        }
    }
}

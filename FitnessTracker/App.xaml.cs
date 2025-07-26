namespace FitnessTrackingApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            

            var stepService = ServiceHelper.GetService<IStepsService>();
            stepService.Start();


            MainPage = new AppShell();
        }
    }
}

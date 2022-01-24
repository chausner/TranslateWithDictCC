using Microsoft.UI.Xaml;
using TranslateWithDictCC.ViewModels;

namespace TranslateWithDictCC
{
    sealed partial class App : Application
    {
        MainWindow mainWindow;

        public App()
        {
            InitializeComponent();
            //Suspending += OnSuspending;

            if (Settings.Instance.AppTheme == ElementTheme.Light)
                RequestedTheme = ApplicationTheme.Light;
            else if (Settings.Instance.AppTheme == ElementTheme.Dark)
                RequestedTheme = ApplicationTheme.Dark;
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            await ApplicationDataMigration.Migrate();
            await DatabaseManager.Instance.InitializeDb();
            await SettingsViewModel.Instance.Load();

            if (!Resources.ContainsKey("settings"))
                Resources.Add("settings", Settings.Instance);

            await DirectionManager.Instance.UpdateDirection();

            mainWindow = new MainWindow();
            mainWindow.Activate();
        }

        /*private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();

            //TODO: Anwendungszustand speichern und alle Hintergrundaktivitäten beenden
            MainViewModel.Instance.SaveSettings();

            deferral.Complete();
        }*/
    }
}

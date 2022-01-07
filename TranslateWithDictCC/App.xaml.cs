using TranslateWithDictCC.ViewModels;
using Microsoft.UI.Xaml;

namespace TranslateWithDictCC
{
    sealed partial class App : Application
    {
        MainWindow mainWindow;

        public App()
        {
            InitializeComponent();
            //Suspending += OnSuspending;            
        }

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs e)
        {
            await ApplicationDataMigration.Migrate();
            await DatabaseManager.Instance.InitializeDb();
            await SettingsViewModel.Instance.Load();

            if (!Resources.ContainsKey("settings"))
                Resources.Add("settings", Settings.Instance);

            await MainViewModel.Instance.UpdateDirection();
            MainViewModel.Instance.LoadSettings();

            mainWindow = new MainWindow();
            //mainWindow.ExtendsContentIntoTitleBar = true;
            mainWindow.Activate();

            SetTitleBarColors();
        }

        /*private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();

            //TODO: Anwendungszustand speichern und alle Hintergrundaktivitäten beenden
            MainViewModel.Instance.SaveSettings();

            deferral.Complete();
        }*/

        private void SetTitleBarColors()
        {
            /*ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;

            titleBar.BackgroundColor = (Color?)Application.Current.Resources["TitleBarBackgroundColor"]; 
            titleBar.ForegroundColor = (Color?)Application.Current.Resources["TitleBarForegroundColor"];
            titleBar.InactiveBackgroundColor = (Color?)Application.Current.Resources["TitleBarInactiveBackgroundColor"];
            titleBar.InactiveForegroundColor = (Color?)Application.Current.Resources["TitleBarInactiveForegroundColor"];
            titleBar.ButtonBackgroundColor = (Color?)Application.Current.Resources["TitleBarButtonBackgroundColor"];
            titleBar.ButtonHoverBackgroundColor = (Color?)Application.Current.Resources["TitleBarButtonHoverBackgroundColor"];
            titleBar.ButtonForegroundColor = (Color?)Application.Current.Resources["TitleBarButtonForegroundColor"];
            titleBar.ButtonHoverForegroundColor = (Color?)Application.Current.Resources["TitleBarButtonHoverForegroundColor"];
            //titleBar.ButtonPressedBackgroundColor =(Color?)Application.Current.Resources["TitleBarButtonPressedBackgroundColor"]; 
            titleBar.ButtonPressedForegroundColor = (Color?)Application.Current.Resources["TitleBarButtonPressedForegroundColor"];
            titleBar.ButtonInactiveBackgroundColor = (Color?)Application.Current.Resources["TitleBarButtonInactiveBackgroundColor"];
            titleBar.ButtonInactiveForegroundColor = (Color?)Application.Current.Resources["TitleBarButtonInactiveForegroundColor"];*/
        }
    }
}

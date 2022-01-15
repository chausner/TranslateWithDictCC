using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Reflection;
using TranslateWithDictCC.ViewModels;
using Windows.UI;
using WinRT.Interop;

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

        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            await ApplicationDataMigration.Migrate();
            await DatabaseManager.Instance.InitializeDb();
            await SettingsViewModel.Instance.Load();

            if (!Resources.ContainsKey("settings"))
                Resources.Add("settings", Settings.Instance);

            await DirectionManager.Instance.UpdateDirection();

            mainWindow = new MainWindow();

            SetTitleBarColorsAndIcon(mainWindow);
            
            mainWindow.Activate();
        }

        /*private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();

            //TODO: Anwendungszustand speichern und alle Hintergrundaktivitäten beenden
            MainViewModel.Instance.SaveSettings();

            deferral.Complete();
        }*/

        private void SetTitleBarColorsAndIcon(Window window)
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(window);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
            AppWindowTitleBar titleBar = appWindow.TitleBar;

            titleBar.BackgroundColor = (Color?)Application.Current.Resources["TitleBarBackgroundColor"]; 
            titleBar.ForegroundColor = (Color?)Application.Current.Resources["TitleBarForegroundColor"];
            titleBar.InactiveBackgroundColor = (Color?)Application.Current.Resources["TitleBarInactiveBackgroundColor"];
            titleBar.InactiveForegroundColor = (Color?)Application.Current.Resources["TitleBarInactiveForegroundColor"];
            titleBar.ButtonBackgroundColor = (Color?)Application.Current.Resources["TitleBarButtonBackgroundColor"];
            titleBar.ButtonHoverBackgroundColor = (Color?)Application.Current.Resources["TitleBarButtonHoverBackgroundColor"];
            titleBar.ButtonForegroundColor = (Color?)Application.Current.Resources["TitleBarButtonForegroundColor"];
            titleBar.ButtonHoverForegroundColor = (Color?)Application.Current.Resources["TitleBarButtonHoverForegroundColor"];
            //titleBar.ButtonPressedBackgroundColor = (Color?)Application.Current.Resources["TitleBarButtonPressedBackgroundColor"]; 
            titleBar.ButtonPressedForegroundColor = (Color?)Application.Current.Resources["TitleBarButtonPressedForegroundColor"];
            titleBar.ButtonInactiveBackgroundColor = (Color?)Application.Current.Resources["TitleBarButtonInactiveBackgroundColor"];
            titleBar.ButtonInactiveForegroundColor = (Color?)Application.Current.Resources["TitleBarButtonInactiveForegroundColor"];

            string applicationRoot = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            appWindow.SetIcon(Path.Combine(applicationRoot, @"Assets\Logo.ico"));
        }
    }
}

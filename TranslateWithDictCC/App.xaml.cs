using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using TranslateWithDictCC.ViewModels;
using WinRT.Interop;

namespace TranslateWithDictCC
{
    sealed partial class App : Application
    {
        MainWindow mainWindow;

        public App()
        {
            InitializeComponent();

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
            await SearchResultsViewModel.Instance.Load();

            if (!Resources.ContainsKey("settings"))
                Resources.Add("settings", Settings.Instance);

            string[] args = Environment.GetCommandLineArgs();
            string launchArguments = args.Length >= 2 ? args[1] : null;

            mainWindow = new MainWindow(launchArguments);

            IntPtr hWnd = WindowNative.GetWindowHandle(mainWindow);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

            mainWindow.Activate();

            appWindow.Closing += AppWindow_Closing;
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            SearchResultsViewModel.Instance.SaveSettings();
        }
    }
}

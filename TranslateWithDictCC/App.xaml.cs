using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Reflection;
using TranslateWithDictCC.ViewModels;
using Windows.Graphics;
using Windows.UI;
using WinRT.Interop;
using WinUIEx;

namespace TranslateWithDictCC
{
    sealed partial class App : Application
    {
        MainWindow mainWindow;

        public App()
        {
            InitializeComponent();
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

            SetTitleBarColorsAndIcon(appWindow);
            SetWindowSizeAndLocation(mainWindow);
            
            mainWindow.Activate();

            appWindow.Closing += AppWindow_Closing;
        }

        private void SetTitleBarColorsAndIcon(AppWindow appWindow)
        {            
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

        private void SetWindowSizeAndLocation(WindowEx window)
        {
            SizeInt32 initialSize = new SizeInt32(1000, 650);
            SizeInt32 minSize = new SizeInt32(680, 430);

            window.CenterOnScreen(initialSize.Width, initialSize.Height);

            // minimum size must be scaled by DPI
            uint dpi = HwndExtensions.GetDpiForWindow(window.GetWindowHandle());
            window.MinWidth = (int)(minSize.Width * dpi / 96.0);
            window.MinHeight = (int)(minSize.Height * dpi / 96.0);
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            SearchResultsViewModel.Instance.SaveSettings();
        }
    }
}

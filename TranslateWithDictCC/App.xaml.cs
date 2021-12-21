using TranslateWithDictCC.ViewModels;
using TranslateWithDictCC.Views;
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace TranslateWithDictCC
{
    sealed partial class App : Application
    {
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

            // new way:
            //m_window = new MainWindow();
            //m_window.Activate();

            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
            {
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;
           
                await MainViewModel.Instance.UpdateDirection();
                MainViewModel.Instance.LoadSettings();

                Window.Current.Content = rootFrame;

                SetTitleBarColors();
            }

            rootFrame.Navigate(typeof(MainPage), e.Arguments);

            Window.Current.Activate();
        }

        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load page " + e.SourcePageType.FullName);
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

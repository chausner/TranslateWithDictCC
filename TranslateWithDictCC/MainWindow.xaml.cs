using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using TranslateWithDictCC.Views;
using Windows.UI;
using WinRT.Interop;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TranslateWithDictCC
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WindowEx
    {
        public static MainWindow Instance { get; private set; }

        public UIElement ApplicationFrame => applicationFrame;

        public MainWindow(string launchArguments)
        {
            this.InitializeComponent();

            Instance = this;

            ResourceLoader resourceLoader = new ResourceLoader();

            Title = resourceLoader.GetString("App_Display_Name");

            SetTheme();
            SetWindowSizeAndLocation();

            applicationFrame.Navigate(typeof(MainPage), launchArguments);

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.AppTheme))
                SetTheme();
        }

        private void SetTheme()
        {
            applicationFrame.RequestedTheme = Settings.Instance.AppTheme;

            SetTitleBarColorsAndIcon();
        }

        private void SetTitleBarColorsAndIcon()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            Microsoft.UI.WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

            // Title bar customization is not supported on Windows 10
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                AppWindowTitleBar titleBar = appWindow.TitleBar;

                ResourceDictionary dictionary = null;

                switch (Settings.Instance.AppTheme)
                {
                    case ElementTheme.Default:
                        dictionary = Application.Current.Resources;
                        break;
                    case ElementTheme.Light:
                        dictionary = (ResourceDictionary)Application.Current.Resources.ThemeDictionaries["Light"];
                        break;
                    case ElementTheme.Dark:
                        dictionary = (ResourceDictionary)Application.Current.Resources.ThemeDictionaries["Dark"];
                        break;
                }

                titleBar.BackgroundColor = (Color?)dictionary["TitleBarBackgroundColor"];
                titleBar.ForegroundColor = (Color?)dictionary["TitleBarForegroundColor"];
                titleBar.InactiveBackgroundColor = (Color?)dictionary["TitleBarInactiveBackgroundColor"];
                titleBar.InactiveForegroundColor = (Color?)dictionary["TitleBarInactiveForegroundColor"];
                titleBar.ButtonBackgroundColor = (Color?)dictionary["TitleBarButtonBackgroundColor"];
                titleBar.ButtonHoverBackgroundColor = (Color?)dictionary["TitleBarButtonHoverBackgroundColor"];
                titleBar.ButtonForegroundColor = (Color?)dictionary["TitleBarButtonForegroundColor"];
                titleBar.ButtonHoverForegroundColor = (Color?)dictionary["TitleBarButtonHoverForegroundColor"];
                titleBar.ButtonPressedBackgroundColor = (Color?)dictionary["TitleBarButtonPressedBackgroundColor"];
                titleBar.ButtonPressedForegroundColor = (Color?)dictionary["TitleBarButtonPressedForegroundColor"];
                titleBar.ButtonInactiveBackgroundColor = (Color?)dictionary["TitleBarButtonInactiveBackgroundColor"];
                titleBar.ButtonInactiveForegroundColor = (Color?)dictionary["TitleBarButtonInactiveForegroundColor"];
            }

            string applicationRoot = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            appWindow.SetIcon(Path.Combine(applicationRoot, @"Assets\Logo.ico"));
        }

        private void SetWindowSizeAndLocation()
        {
            this.CenterOnScreen(1000, 650);

            MinWidth = 680;
            MinHeight = 430;
        }
    }
}

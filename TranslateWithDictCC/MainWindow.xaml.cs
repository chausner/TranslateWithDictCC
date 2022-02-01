using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using TranslateWithDictCC.Views;
using Windows.Graphics;
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
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
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
            //titleBar.ButtonPressedBackgroundColor = (Color?)dictionary["TitleBarButtonPressedBackgroundColor"]; 
            titleBar.ButtonPressedForegroundColor = (Color?)dictionary["TitleBarButtonPressedForegroundColor"];
            titleBar.ButtonInactiveBackgroundColor = (Color?)dictionary["TitleBarButtonInactiveBackgroundColor"];
            titleBar.ButtonInactiveForegroundColor = (Color?)dictionary["TitleBarButtonInactiveForegroundColor"];

            string applicationRoot = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            appWindow.SetIcon(Path.Combine(applicationRoot, @"Assets\Logo.ico"));
        }

        private void SetWindowSizeAndLocation()
        {
            SizeInt32 initialSize = new SizeInt32(1000, 650);
            SizeInt32 minSize = new SizeInt32(680, 430);

            this.CenterOnScreen(initialSize.Width, initialSize.Height);

            // minimum size must be scaled by DPI
            uint dpi = HwndExtensions.GetDpiForWindow(this.GetWindowHandle());
            MinWidth = (int)(minSize.Width * dpi / 96.0);
            MinHeight = (int)(minSize.Height * dpi / 96.0);
        }
    }
}

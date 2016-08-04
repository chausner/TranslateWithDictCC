using System;
using System.Reflection;
using Windows.ApplicationModel.Resources;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace TranslateWithDictCC.Views
{
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            InitializeComponent();

            ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

            versionTextBlock.Text = string.Format(resourceLoader.GetString("AboutPage_Version"), GetType().GetTypeInfo().Assembly.GetName().Version);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("mailto:translatewithdict.cc@outlook.com"));
        }
    }
}

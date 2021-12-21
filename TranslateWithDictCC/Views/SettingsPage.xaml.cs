using TranslateWithDictCC.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace TranslateWithDictCC.Views
{
    public sealed partial class SettingsPage : Page
    {
        SettingsViewModel ViewModel { get { return SettingsViewModel.Instance; } }

        Settings Settings { get { return Settings.Instance; } }

        public SettingsPage()
        {
            InitializeComponent();

            DataContext = SettingsViewModel.Instance;
        }
    }
}

using TranslateWithDictCC.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace TranslateWithDictCC.Views
{
    public sealed partial class SettingsPage : Page
    {
        SettingsViewModel ViewModel => SettingsViewModel.Instance;

        Settings Settings => Settings.Instance;

        public SettingsPage()
        {
            InitializeComponent();

            DataContext = SettingsViewModel.Instance;
        }
    }
}

using TranslateWithDictCC.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace TranslateWithDictCC.Views
{
    public sealed partial class AboutPage : Page
    {
        AboutViewModel ViewModel { get; }

        public AboutPage()
        {
            InitializeComponent();

            ViewModel = new AboutViewModel();
        }
    }
}

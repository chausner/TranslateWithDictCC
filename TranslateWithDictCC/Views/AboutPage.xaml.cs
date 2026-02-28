using Microsoft.UI.Xaml.Controls;
using TranslateWithDictCC.ViewModels;

namespace TranslateWithDictCC.Views;

public sealed partial class AboutPage : Page
{
    AboutViewModel ViewModel { get; }

    public AboutPage()
    {
        InitializeComponent();

        ViewModel = new AboutViewModel();
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using TranslateWithDictCC.ViewModels;

namespace TranslateWithDictCC.Views;

public sealed partial class SettingsPage : Page
{
    SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        InitializeComponent();

        ViewModel = ((App)App.Current).Host.Services.GetRequiredService<SettingsViewModel>();

        DataContext = ViewModel;
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace TranslateWithDictCC.ViewModels;

partial class MainViewModel : ObservableObject
{
    public static readonly MainViewModel Instance = new MainViewModel();

    [ObservableProperty]
    public partial bool ShowNoDictionaryInstalledTeachingTip { get; set; }

    public bool NoDictionaryInstalledTeachingTipShown { get; set; }

    public ICommand NavigateToPageCommand { get; set; } = null!;
    public ICommand GoBackToPageCommand { get; set; } = null!;

    private MainViewModel()
    {
    }
}

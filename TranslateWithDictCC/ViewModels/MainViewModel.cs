using System.Windows.Input;

namespace TranslateWithDictCC.ViewModels;

class MainViewModel : ViewModel
{
    public static readonly MainViewModel Instance = new MainViewModel();

    public bool ShowNoDictionaryInstalledTeachingTip
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool NoDictionaryInstalledTeachingTipShown { get; set; }

    public ICommand NavigateToPageCommand { get; set; } = null!;
    public ICommand GoBackToPageCommand { get; set; } = null!;

    private MainViewModel()
    {
    }
}

using System.Windows.Input;

namespace TranslateWithDictCC.ViewModels;

class MainViewModel : ViewModel
{
    public static readonly MainViewModel Instance = new MainViewModel();

    bool showNoDictionaryInstalledTeachingTip;

    public bool ShowNoDictionaryInstalledTeachingTip
    {
        get { return showNoDictionaryInstalledTeachingTip; }
        set { SetProperty(ref showNoDictionaryInstalledTeachingTip, value); }
    }

    public bool NoDictionaryInstalledTeachingTipShown { get; set; }

    public ICommand NavigateToPageCommand { get; set; }
    public ICommand GoBackToPageCommand { get; set; }

    private MainViewModel()
    {
    }
}

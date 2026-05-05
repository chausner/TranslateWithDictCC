namespace TranslateWithDictCC.ViewModels;

class MainViewModel : ViewModel
{
    public bool ShowNoDictionaryInstalledTeachingTip
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool NoDictionaryInstalledTeachingTipShown { get; set; }
}

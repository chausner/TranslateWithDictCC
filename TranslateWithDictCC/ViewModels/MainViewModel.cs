using CommunityToolkit.Mvvm.ComponentModel;

namespace TranslateWithDictCC.ViewModels;

partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool ShowNoDictionaryInstalledTeachingTip { get; set; }

    public bool NoDictionaryInstalledTeachingTipShown { get; set; }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Windows.Input;
using TranslateWithDictCC.Models;

namespace TranslateWithDictCC.ViewModels;

partial class DictionaryViewModel : ObservableObject
{
    readonly SettingsViewModel settingsViewModel;

    public string OriginLanguageCode { get; } = null!;
    public string DestinationLanguageCode { get; } = null!;

    public string OriginLanguage => LanguageCodes.GetLanguageName(OriginLanguageCode);
    public string DestinationLanguage => LanguageCodes.GetLanguageName(DestinationLanguageCode);

    public BitmapImage OriginLanguageImage => LanguageCodes.GetCountryFlagImage(OriginLanguageCode);
    public BitmapImage DestinationLanguageImage => LanguageCodes.GetCountryFlagImage(DestinationLanguageCode);

    public DateTimeOffset CreationDate { get; }

    public string CreationDateShort => CreationDate.ToString("dd/MM/yyyy");

    [ObservableProperty]
    public partial int NumberOfEntries { get; set; }

    [ObservableProperty]
    public partial DictionaryStatus Status { get; set; }

    [ObservableProperty]
    public partial string StatusText { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial double ImportProgress { get; set; }

    [ObservableProperty]
    public partial Visibility ProgressBarVisibility { get; private set; }

    [ObservableProperty]
    public partial Visibility AbortImportButtonVisibility { get; private set; }

    [ObservableProperty]
    public partial Visibility RemoveDictionaryButtonVisibility { get; private set; }

    public Dictionary? Dictionary { get; set; }
    public WordlistReader? WordlistReader { get; set; }

    public ICommand AbortImportCommand { get; }
    public ICommand RemoveDictionaryCommand { get; }

    private DictionaryViewModel(SettingsViewModel settingsViewModel)
    {
        this.settingsViewModel = settingsViewModel;

        PropertyChanged += (sender, e) => { UpdateStatusText(); };

        AbortImportCommand = new RelayCommand(RunAbortImportCommand);
        RemoveDictionaryCommand = new RelayCommand(RunRemoveDictionaryCommand);
    }

    public DictionaryViewModel(Dictionary dictionary, SettingsViewModel settingsViewModel) : this(settingsViewModel)
    {
        Dictionary = dictionary;
        OriginLanguageCode = dictionary.OriginLanguageCode;
        DestinationLanguageCode = dictionary.DestinationLanguageCode;
        CreationDate = dictionary.CreationDate;
        NumberOfEntries = dictionary.NumberOfEntries;
        Status = DictionaryStatus.Installed;

        UpdateStatusText();
    }

    public DictionaryViewModel(WordlistReader wordlistReader, SettingsViewModel settingsViewModel) : this(settingsViewModel)
    {
        WordlistReader = wordlistReader;
        OriginLanguageCode = wordlistReader.OriginLanguageCode;
        DestinationLanguageCode = wordlistReader.DestinationLanguageCode;
        CreationDate = wordlistReader.CreationDate;
        NumberOfEntries = 0;
        Status = DictionaryStatus.Queued;

        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        ResourceLoader resourceLoader = new ResourceLoader();

        switch (Status)
        {
            case DictionaryStatus.Queued:
                StatusText = resourceLoader.GetString("DictionaryStatus_Queued");
                ProgressBarVisibility = Visibility.Collapsed;
                AbortImportButtonVisibility = Visibility.Collapsed;
                RemoveDictionaryButtonVisibility = Visibility.Collapsed;
                break;
            case DictionaryStatus.Installing:
                StatusText = resourceLoader.GetString("DictionaryStatus_Installing");
                ProgressBarVisibility = Visibility.Visible;
                AbortImportButtonVisibility = Visibility.Visible;
                RemoveDictionaryButtonVisibility = Visibility.Collapsed;
                break;
            case DictionaryStatus.Installed:
                StatusText = string.Format(resourceLoader.GetString("DictionaryStatus_Installed"), NumberOfEntries);
                ProgressBarVisibility = Visibility.Collapsed;
                AbortImportButtonVisibility = Visibility.Collapsed;
                RemoveDictionaryButtonVisibility = Visibility.Visible;
                break;
        }
    }

    private void RunAbortImportCommand()
    {
        settingsViewModel.AbortImport(this);
    }

    private async void RunRemoveDictionaryCommand()
    {
        await settingsViewModel.RemoveDictionary(this);
    }
}

enum DictionaryStatus
{
    Queued,
    Installing,
    Installed
}

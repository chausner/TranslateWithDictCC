using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Windows.Input;
using TranslateWithDictCC.Models;

namespace TranslateWithDictCC.ViewModels;

class DictionaryViewModel : ViewModel
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

    public int NumberOfEntries
    {
        get;
        set => SetProperty(ref field, value);
    }

    public DictionaryStatus Status
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string StatusText
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public double ImportProgress
    {
        get;
        set => SetProperty(ref field, value);
    }

    public Visibility ProgressBarVisibility
    {
        get;
        set => SetProperty(ref field, value);
    }

    public Visibility AbortImportButtonVisibility
    {
        get;
        set => SetProperty(ref field, value);
    }

    public Visibility RemoveDictionaryButtonVisibility
    {
        get;
        set => SetProperty(ref field, value);
    }

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

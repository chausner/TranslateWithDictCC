using TranslateWithDictCC.Models;
using System;
using Microsoft.Windows.ApplicationModel.Resources;
using System.Windows.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml;

namespace TranslateWithDictCC.ViewModels
{
    class DictionaryViewModel : ViewModel
    {
        public string OriginLanguageCode { get; }
        public string DestinationLanguageCode { get; }

        public string OriginLanguage => LanguageCodes.GetLanguageName(OriginLanguageCode);
        public string DestinationLanguage => LanguageCodes.GetLanguageName(DestinationLanguageCode);

        public BitmapImage OriginLanguageImage => LanguageCodes.GetCountryFlagImage(OriginLanguageCode);
        public BitmapImage DestinationLanguageImage => LanguageCodes.GetCountryFlagImage(DestinationLanguageCode);

        public DateTimeOffset CreationDate { get; }

        public string CreationDateShort => CreationDate.ToString("dd/MM/yyyy");

        int numberOfEntries;

        public int NumberOfEntries
        {
            get { return numberOfEntries; }
            set { SetProperty(ref numberOfEntries, value); }
        }

        DictionaryStatus status;

        public DictionaryStatus Status
        {
            get { return status; }
            set { SetProperty(ref status, value); }
        }

        string statusText;

        public string StatusText
        {
            get { return statusText; }
            set { SetProperty(ref statusText, value); }
        }

        double importProgress;

        public double ImportProgress
        {
            get { return importProgress; }
            set { SetProperty(ref importProgress, value); }
        }

        Visibility progressBarVisibility;

        public Visibility ProgressBarVisibility
        {
            get { return progressBarVisibility; }
            set { SetProperty(ref progressBarVisibility, value); }
        }

        public Dictionary Dictionary { get; set; }
        public WordlistReader WordlistReader { get; set; }

        public ICommand RemoveDictionaryCommand { get; }

        private DictionaryViewModel()
        {
            PropertyChanged += (sender, e) => { UpdateStatusText(); };

            RemoveDictionaryCommand = new RelayCommand(RunRemoveDictionaryCommand);
        }

        public DictionaryViewModel(Dictionary dictionary) : this()
        {
            Dictionary = dictionary;
            OriginLanguageCode = dictionary.OriginLanguageCode;
            DestinationLanguageCode = dictionary.DestinationLanguageCode;
            CreationDate = dictionary.CreationDate;
            NumberOfEntries = dictionary.NumberOfEntries;
            Status = DictionaryStatus.Installed;
        }

        public DictionaryViewModel(WordlistReader wordlistReader) : this()
        {
            WordlistReader = wordlistReader;
            OriginLanguageCode = wordlistReader.OriginLanguageCode;
            DestinationLanguageCode = wordlistReader.DestinationLanguageCode;
            CreationDate = wordlistReader.CreationDate;
            NumberOfEntries = 0;
            Status = DictionaryStatus.Queued;
        }

        private void UpdateStatusText()
        {
            ResourceLoader resourceLoader = new ResourceLoader();

            switch (Status)
            {
                case DictionaryStatus.Queued:
                    StatusText = resourceLoader.GetString("DictionaryStatus_Queued");
                    ProgressBarVisibility = Visibility.Collapsed;
                    break;
                case DictionaryStatus.Installing:
                    StatusText = resourceLoader.GetString("DictionaryStatus_Installing");
                    ProgressBarVisibility = Visibility.Visible;
                    break;
                case DictionaryStatus.Installed:
                    StatusText = string.Format(resourceLoader.GetString("DictionaryStatus_Installed"), NumberOfEntries);
                    ProgressBarVisibility = Visibility.Collapsed;
                    break;
            }
        }

        private async void RunRemoveDictionaryCommand()
        {
            await SettingsViewModel.Instance.RemoveDictionary(this, false);
        }
    }

    enum DictionaryStatus
    {
        Queued,
        Installing,
        Installed
    }
}

using TranslateWithDictCC.Models;
using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;

namespace TranslateWithDictCC.ViewModels
{
    class DictionaryViewModel : ViewModel
    {
        public string OriginLanguageCode { get; }
        public string DestinationLanguageCode { get; }
        public string OriginLanguage { get { return LanguageCodes.GetLanguageName(OriginLanguageCode); } }
        public string DestinationLanguage { get { return LanguageCodes.GetLanguageName(DestinationLanguageCode); } }

        DateTimeOffset creationDate;

        public DateTimeOffset CreationDate
        {
            get { return creationDate; }
            set { SetProperty(ref creationDate, value); }
        }

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

        private DictionaryViewModel()
        {
            PropertyChanged += (sender, e) => { UpdateStatusText(); };
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
            ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

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
    }

    enum DictionaryStatus
    {
        Queued,
        Installing,
        Installed
    }
}

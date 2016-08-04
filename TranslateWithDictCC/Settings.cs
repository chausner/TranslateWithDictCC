using TranslateWithDictCC.ViewModels;
using System;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace TranslateWithDictCC
{
    class Settings : ViewModel
    {
        public static readonly Settings Instance = new Settings();

        ApplicationDataCompositeValue selectedDirection;

        public void GetSelectedDirection(out string originLanguageCode, out string destinationLanguageCode)
        {
            if (selectedDirection != null)
            {
                originLanguageCode = selectedDirection["OriginLanguageCode"] as string;
                destinationLanguageCode = selectedDirection["DestinationLanguageCode"] as string;
            }
            else
            {
                originLanguageCode = null;
                destinationLanguageCode = null;
            }
        }

        public void SetSelectedDirection(string originLanguageCode, string destinationLanguageCode)
        {
            if (originLanguageCode != null && destinationLanguageCode != null)
            {
                selectedDirection = new ApplicationDataCompositeValue();

                selectedDirection["OriginLanguageCode"] = originLanguageCode;
                selectedDirection["DestinationLanguageCode"] = destinationLanguageCode;
            }
            else
                selectedDirection = null;

            ApplicationData.Current.LocalSettings.Values["SelectedDirection"] = selectedDirection;
        }

        bool searchInBothDirections;

        public bool SearchInBothDirections
        {
            get
            {
                return searchInBothDirections;
            }
            set
            {
                if (value != searchInBothDirections)
                {
                    ApplicationData.Current.LocalSettings.Values["SearchInBothDirections"] = value;
                    SetProperty(ref searchInBothDirections, value);
                }
            }
        }

        bool caseSensitiveSearch;

        public bool CaseSensitiveSearch
        {
            get
            {
                return caseSensitiveSearch;
            }
            set
            {
                if (value != caseSensitiveSearch)
                {
                    ApplicationData.Current.LocalSettings.Values["CaseSensitiveSearch"] = value;
                    SetProperty(ref caseSensitiveSearch, value);
                }
            }
        }

        bool showAudioRecordingButton;

        public bool ShowAudioRecordingButton
        {
            get
            {
                return showAudioRecordingButton;
            }
            set
            {
                if (value != showAudioRecordingButton)
                {
                    ApplicationData.Current.LocalSettings.Values["ShowAudioRecordingButton"] = value;
                    SetProperty(ref showAudioRecordingButton, value);                    
                }
            }
        }

        bool showWordClasses;

        public bool ShowWordClasses
        {
            get
            {
                return showWordClasses;
            }
            set
            {
                if (value != showWordClasses)
                {
                    ApplicationData.Current.LocalSettings.Values["ShowWordClasses"] = value;
                    SetProperty(ref showWordClasses, value);
                }
            }
        }

        private Settings()
        {
            IPropertySet settingsValues = ApplicationData.Current.LocalSettings.Values;

            selectedDirection = settingsValues["SelectedDirection"] as ApplicationDataCompositeValue;
            searchInBothDirections = (settingsValues["SearchInBothDirections"] as bool?).GetValueOrDefault(true);
            caseSensitiveSearch = (settingsValues["CaseSensitiveSearch"] as bool?).GetValueOrDefault(true);
            showAudioRecordingButton = (settingsValues["ShowAudioRecordingButton"] as bool?).GetValueOrDefault(true);
            showWordClasses = (settingsValues["ShowWordClasses"] as bool?).GetValueOrDefault(true);
        }     
    }
}

﻿using Microsoft.UI.Xaml;
using System;
using TranslateWithDictCC.ViewModels;
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

        bool showSubjects;

        public bool ShowSubjects
        {
            get
            {
                return showSubjects;
            }
            set
            {
                if (value != showSubjects)
                {
                    ApplicationData.Current.LocalSettings.Values["ShowSubjects"] = value;
                    SetProperty(ref showSubjects, value);
                }
            }
        }

        ElementTheme appTheme;

        public ElementTheme AppTheme
        {
            get
            {
                return appTheme;
            }
            set
            {
                if (value != appTheme)
                {
                    ApplicationData.Current.LocalSettings.Values["AppTheme"] = Enum.GetName(value);
                    SetProperty(ref appTheme, value);
                }
            }
        }

        bool outdatedDictionariesNoticeRead;

		public bool OutdatedDictionariesNoticeRead
		{
			get
			{
				return outdatedDictionariesNoticeRead;
			}
			set
			{
				if (value != outdatedDictionariesNoticeRead)
				{
					ApplicationData.Current.LocalSettings.Values["OutdatedDictionariesNoticeRead"] = value;
					SetProperty(ref outdatedDictionariesNoticeRead, value);
				}
			}
		}

		private Settings()
        {
            IPropertySet settingsValues = ApplicationData.Current.LocalSettings.Values;

            selectedDirection = settingsValues["SelectedDirection"] as ApplicationDataCompositeValue;
            caseSensitiveSearch = (settingsValues["CaseSensitiveSearch"] as bool?).GetValueOrDefault(true);
            showWordClasses = (settingsValues["ShowWordClasses"] as bool?).GetValueOrDefault(true);
            showSubjects = (settingsValues["ShowSubjects"] as bool?).GetValueOrDefault(true);
            appTheme = Enum.Parse<ElementTheme>((settingsValues["AppTheme"] as string) ?? Enum.GetName(ElementTheme.Default));
			outdatedDictionariesNoticeRead = (settingsValues["OutdatedDictionariesNoticeRead"] as bool?).GetValueOrDefault(false);
		}     
    }
}

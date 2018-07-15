using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Media.Imaging;

namespace TranslateWithDictCC
{
    static class LanguageCodes
    {
        static ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();        

        static Dictionary<string, string> countryCodes = new Dictionary<string, string> {
            { "BG", "BG" },
            { "BS", "BA" },
            { "CS", "CZ" },
            { "DA", "DK" },
            { "DE", "DE" },
            { "EL", "GR" },
            { "EN", "GB" },
            //{ "EO", "" },
            { "ES", "ES" },
            { "FI", "FI" },
            { "FR", "FR" },
            { "HR", "HR" },
            { "HU", "HU" },
            { "IS", "IS" },
            { "IT", "IT" },
            //{ "LA", "" },
            { "NL", "NL" },
            { "NO", "NO" },
            { "PL", "PL" },
            { "PT", "PT" },
            { "RO", "RO" },
            { "RU", "RU" },
            { "SK", "SK" },
            { "SQ", "AL" },
            { "SR", "RS" },
            { "SV", "SE" },
            { "TR", "TR" }
        };

        static Dictionary<string, BitmapImage> flagImages = new Dictionary<string, BitmapImage>();

        public static string GetLanguageName(string languageCode)
        {
            string languageName = resourceLoader.GetString("Language_Name_" + languageCode);

            if (!string.IsNullOrEmpty(languageName))
                return languageName;
            else
                return languageCode;
        }

        public static Uri GetCountryFlagUri(string languageCode)
        {
            if (countryCodes.TryGetValue(languageCode, out string countryCode))
                return new Uri("ms-appx:///Assets/Flags/" + countryCode + ".ico");
            else
                return new Uri("ms-appx:///Assets/Flags/_unknown.ico");
        }

        public static BitmapImage GetCountryFlagImage(string languageCode)
        {
            if (flagImages.TryGetValue(languageCode, out BitmapImage countryFlagImage))
                return countryFlagImage;
            else
            {
                countryFlagImage = new BitmapImage(GetCountryFlagUri(languageCode));
                flagImages.Add(languageCode, countryFlagImage);
                return countryFlagImage;
            }
        }
    }
}

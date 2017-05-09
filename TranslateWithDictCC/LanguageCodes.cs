using System.Collections.Generic;
using Windows.ApplicationModel.Resources;

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

        public static string GetLanguageName(string languageCode)
        {
            string languageName = resourceLoader.GetString("Language_Name_" + languageCode);

            if (!string.IsNullOrEmpty(languageName))
                return languageName;
            else
                return languageCode;
        }

        public static string GetCountryFlagUri(string languageCode)
        {
            if (countryCodes.TryGetValue(languageCode, out string countryCode))
                return "/Assets/Flags/" + countryCode + ".ico";
            else
                return "/Assets/Flags/_unknown.ico";
        }
    }
}

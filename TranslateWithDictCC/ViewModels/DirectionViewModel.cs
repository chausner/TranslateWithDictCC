using TranslateWithDictCC.Models;

namespace TranslateWithDictCC.ViewModels
{
    class DirectionViewModel
    {
        public Dictionary Dictionary { get; }
        public bool ReverseSearch { get; }

        public string OriginLanguage { get { return LanguageCodes.GetLanguageName(OriginLanguageCode); } }
        public string OriginLanguageCode { get { return ReverseSearch ? Dictionary.DestinationLanguageCode : Dictionary.OriginLanguageCode; } }
        public string DestinationLanguage { get { return LanguageCodes.GetLanguageName(DestinationLanguageCode); } }
        public string DestinationLanguageCode { get { return ReverseSearch ? Dictionary.OriginLanguageCode : Dictionary.DestinationLanguageCode; } }

        public DirectionViewModel(Dictionary dictionary, bool reverseSearch)
        {
            Dictionary = dictionary;
            ReverseSearch = reverseSearch;
        }
    }
}

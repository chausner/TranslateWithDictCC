using Microsoft.UI.Xaml.Media.Imaging;
using TranslateWithDictCC.Models;

namespace TranslateWithDictCC.ViewModels
{
    class DirectionViewModel
    {
        public Dictionary Dictionary { get; }
        public bool ReverseSearch { get; }

        public string OriginLanguage => LanguageCodes.GetLanguageName(OriginLanguageCode);
        public string DestinationLanguage => LanguageCodes.GetLanguageName(DestinationLanguageCode);

        public string OriginLanguageCode => ReverseSearch ? Dictionary.DestinationLanguageCode : Dictionary.OriginLanguageCode;
        public string DestinationLanguageCode => ReverseSearch ? Dictionary.OriginLanguageCode : Dictionary.DestinationLanguageCode;

        public BitmapImage OriginLanguageImage => LanguageCodes.GetCountryFlagImage(OriginLanguageCode);
        public BitmapImage DestinationLanguageImage => LanguageCodes.GetCountryFlagImage(DestinationLanguageCode);

        public DirectionViewModel(Dictionary dictionary, bool reverseSearch)
        {
            Dictionary = dictionary;
            ReverseSearch = reverseSearch;
        }

        public override bool Equals(object obj)
        {
            DirectionViewModel other = obj as DirectionViewModel;

            if (other == null)
                return false;

            return OriginLanguageCode == other.OriginLanguageCode && DestinationLanguageCode == other.DestinationLanguageCode;
        }

        public override int GetHashCode()
        {
            return unchecked(7 * OriginLanguageCode.GetHashCode() + DestinationLanguageCode.GetHashCode());
        }

        public bool EqualsReversed(DirectionViewModel directionViewModel)
        {
            if (directionViewModel == null)
                return false;

            return OriginLanguageCode == directionViewModel.DestinationLanguageCode &&
                DestinationLanguageCode == directionViewModel.OriginLanguageCode;
        }
    }
}

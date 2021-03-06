﻿using System;
using TranslateWithDictCC.Models;
using Windows.UI.Xaml.Media.Imaging;

namespace TranslateWithDictCC.ViewModels
{
    class DirectionViewModel
    {
        public Dictionary Dictionary { get; }
        public bool ReverseSearch { get; }

        public string OriginLanguage { get { return LanguageCodes.GetLanguageName(OriginLanguageCode); } }
        public string DestinationLanguage { get { return LanguageCodes.GetLanguageName(DestinationLanguageCode); } }

        public string OriginLanguageCode { get { return ReverseSearch ? Dictionary.DestinationLanguageCode : Dictionary.OriginLanguageCode; } }
        public string DestinationLanguageCode { get { return ReverseSearch ? Dictionary.OriginLanguageCode : Dictionary.DestinationLanguageCode; } }

        public BitmapImage OriginLanguageImage { get { return LanguageCodes.GetCountryFlagImage(OriginLanguageCode); } }
        public BitmapImage DestinationLanguageImage { get { return LanguageCodes.GetCountryFlagImage(DestinationLanguageCode); } }

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

using System;
using System.Collections;
using Windows.UI.Xaml.Data;

namespace TranslateWithDictCC
{
    class LanguageIconUrlValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string languageCode = value as string;

            if (languageCode == null)
                return null;

            return LanguageCodes.GetCountryFlagUri(languageCode);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }

    class LanguageNameValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string languageCode = value as string;

            if (languageCode == null)
                return null;

            return LanguageCodes.GetLanguageName(languageCode);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }

    class SourceNotEmptyValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            IEnumerable enumerable = value as IEnumerable;

            if (enumerable == null)
                return null;

            return enumerable.GetEnumerator().MoveNext();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}

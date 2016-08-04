using TranslateWithDictCC.ViewModels;
using System;
using System.Collections;
using Windows.UI.Xaml;
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

    class FormatValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string formatString = parameter as string;

            if (!string.IsNullOrEmpty(formatString))
                return string.Format(formatString, value);
            else if (value != null)
                return value.ToString();
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }

    class WordClassesVisiblityValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string wordClasses = value as string;

            if (!string.IsNullOrEmpty(wordClasses))
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }

    class BooleanVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (!(value is bool))
                return null;

            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (!(value is Visibility))
                return null;

            return (Visibility)value == Visibility.Visible;
        }
    }

    class AudioRecordingButtonStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (!(value is AudioRecordingState))
                return null;

            AudioRecordingState state = (AudioRecordingState)value;

            switch (state)
            {
                case AudioRecordingState.Available:
                    return (char)0xE767;
                case AudioRecordingState.Playing:
                    return (char)0xE769; // 0xE71A
                case AudioRecordingState.Unavailable:
                    return (char)0xE74F;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}

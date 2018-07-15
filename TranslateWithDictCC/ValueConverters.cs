using System;
using System.Collections;
using Windows.UI.Xaml.Data;

namespace TranslateWithDictCC
{
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

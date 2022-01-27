using Microsoft.UI.Xaml.Data;
using System;
using System.Collections;

namespace TranslateWithDictCC
{
    class SourceNotEmptyValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is IEnumerable enumerable)
                return enumerable.GetEnumerator().MoveNext();
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}

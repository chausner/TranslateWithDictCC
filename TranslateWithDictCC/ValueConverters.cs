using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections;

namespace TranslateWithDictCC;

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

class AppThemeValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ElementTheme appTheme)
            return Enum.GetName(appTheme);
        else
            return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is string appTheme)
            return Enum.Parse<ElementTheme>(appTheme);
        else
            return null;
    }
}

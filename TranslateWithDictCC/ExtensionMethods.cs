using System;
using Windows.UI.Xaml.Controls;

namespace TranslateWithDictCC
{
    static class ExtensionMethods
    {
        public static void NavigateIfNeeded(this Frame frame, Type sourcePageType, object parameter = null)
        {
            if (frame.SourcePageType != sourcePageType)
                frame.Navigate(sourcePageType, parameter);
        }
    }
}

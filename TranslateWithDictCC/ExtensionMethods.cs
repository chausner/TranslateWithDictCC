using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TranslateWithDictCC
{
    static class ExtensionMethods
    {
        public static void NavigateIfNeeded(this Frame frame, Type sourcePageType, object parameter = null, NavigationTransitionInfo transitionInfo = null)
        {
            if (frame.SourcePageType != sourcePageType)
                frame.Navigate(sourcePageType, parameter, transitionInfo);
        }
    }
}

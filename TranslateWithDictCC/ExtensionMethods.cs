using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;

namespace TranslateWithDictCC;

static class ExtensionMethods
{
    extension(Frame frame)
    {
        public void NavigateIfNeeded(Type sourcePageType, object? parameter = null, NavigationTransitionInfo? transitionInfo = null)
        {
            if (frame.SourcePageType != sourcePageType)
                frame.Navigate(sourcePageType, parameter, transitionInfo);
        }
    }
}

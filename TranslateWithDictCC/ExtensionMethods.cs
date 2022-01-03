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

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> enumerable, Func<TSource, TKey> selector)
        {
            return enumerable.Distinct(new DistinctEqualityComparer<TSource, TKey>(selector));
        }

        private class DistinctEqualityComparer<TSource, TKey> : EqualityComparer<TSource>
        {
            Func<TSource, TKey> selector;

            public DistinctEqualityComparer(Func<TSource, TKey> selector)
            {
                this.selector = selector;
            }

            public override bool Equals(TSource x, TSource y)
            {
                return selector(x).Equals(selector(y));
            }

            public override int GetHashCode(TSource obj)
            {
                return selector(obj).GetHashCode();
            }
        }
    }
}

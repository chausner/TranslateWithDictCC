using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TranslateWithDictCC.Models;

namespace TranslateWithDictCC.ViewModels
{
    static class WordHighlighting
    {
        static readonly SolidColorBrush annotationBrush = (SolidColorBrush)Application.Current.Resources["DictionaryEntryAnnotationThemeBrush"];
        static readonly SolidColorBrush queryHighlightBrush = (SolidColorBrush)Application.Current.Resources["DictionaryEntryQueryHighlightThemeBrush"];

        public static Block GenerateRichTextBlock(string word, TextSpan[] matchSpans, bool highlightQuery)
        {
            TextSpan[] annotationSpans = GetAnnotationSpans(word);

            Paragraph paragraph = new Paragraph();

            TextSpan lastSpan = new TextSpan();

            int betweenOffset;
            int betweenLength;

            if (highlightQuery)
            {
                foreach (TextSpan span in MergeSpans(matchSpans, word))
                {
                    betweenOffset = lastSpan.Offset + lastSpan.Length;
                    betweenLength = span.Offset - betweenOffset;

                    if (betweenLength != 0)
                        GenerateRun(paragraph, word, betweenOffset, betweenLength, annotationSpans);

                    GenerateQueryHighlight(paragraph, word, annotationSpans, span);

                    lastSpan = span;
                }
            }

            betweenOffset = lastSpan.Offset + lastSpan.Length;
            betweenLength = word.Length - betweenOffset;

            if (betweenLength != 0)
                GenerateRun(paragraph, word, betweenOffset, betweenLength, annotationSpans);

            return paragraph;
        }

        private static IReadOnlyList<TextSpan> MergeSpans(TextSpan[] matchSpans, string word)
        {
            List<TextSpan> mergedSpans = new List<TextSpan>(matchSpans.Length);

            TextSpan currentSpan = matchSpans[0];

            for (int i = 1; i < matchSpans.Length; i++)
            {
                bool onlyWhitespaceBetweenSpans = true;
                for (int j = currentSpan.Offset + currentSpan.Length; j < matchSpans[i].Offset; j++)
                {
                    if (!char.IsWhiteSpace(word[j]))
                    {
                        onlyWhitespaceBetweenSpans = false;
                        break;
                    }
                }

                if (onlyWhitespaceBetweenSpans)
                    currentSpan.Length = matchSpans[i].Offset + matchSpans[i].Length - currentSpan.Offset;
                else
                {
                    mergedSpans.Add(currentSpan);
                    currentSpan = matchSpans[i];
                }
            }

            mergedSpans.Add(currentSpan);

            return mergedSpans;
        }

        private static void GenerateRun(Paragraph paragraph, string word, int offset, int length, TextSpan[] annotationSpans)
        {
            IEnumerable<TextSpan> affectedAnnotationSpans =
                annotationSpans
                .Where(span => span.Intersects(new TextSpan(offset, length)))
                .Select(span => new TextSpan(Math.Max(span.Offset, offset), Math.Min(span.Offset + span.Length, offset + length) - Math.Max(span.Offset, offset)));

            TextSpan lastSpan = new TextSpan(offset, 0);

            int betweenOffset;
            int betweenLength;

            foreach (TextSpan span in affectedAnnotationSpans)
            {
                betweenOffset = lastSpan.Offset + lastSpan.Length;
                betweenLength = span.Offset - betweenOffset;

                if (betweenLength != 0)
                    AddRun(paragraph, word, betweenOffset, betweenLength, false);

                AddRun(paragraph, word, span.Offset, span.Length, true);

                lastSpan = span;
            }

            betweenOffset = lastSpan.Offset + lastSpan.Length;
            betweenLength = offset + length - betweenOffset;

            if (betweenLength != 0)
                AddRun(paragraph, word, betweenOffset, betweenLength, false);
        }

        private static void AddRun(Paragraph paragraph, string word, int offset, int length, bool annotation)
        {
            Run run = new Run() { Text = word.Substring(offset, length) };

            if (annotation)
                ApplyAnnotationStyle(run);
            else
                ApplyQueryHighlightStyle(run);

            paragraph.Inlines.Add(run);
        }

        private static void GenerateQueryHighlight(Paragraph paragraph, string word, TextSpan[] annotationSpans, TextSpan span)
        {
            Rectangle rectangle = new Rectangle() { RadiusX = 4, RadiusY = 4, Fill = queryHighlightBrush };
            TextBlock textBlock = new TextBlock() { Text = word.Substring(span.Offset, span.Length) };
            textBlock.Margin = new Thickness(4, 0, 4, 0);

            if (annotationSpans.Any(annotationSpan => annotationSpan.Contains(span)))
                ApplyAnnotationStyle(textBlock);
            else
                ApplyQueryHighlightStyle(textBlock);

            Grid grid = new Grid();
            grid.Children.Add(rectangle);
            grid.Children.Add(textBlock);
            grid.Margin = new Thickness(0, -4, 0, 0);
            grid.RenderTransform = new TranslateTransform() { Y = 4 };

            paragraph.Inlines.Add(new InlineUIContainer() { Child = grid });
        }

        private static void ApplyAnnotationStyle(Run run)
        {
            run.Foreground = annotationBrush;
        }

        private static void ApplyAnnotationStyle(TextBlock textBlock)
        {
            textBlock.Foreground = annotationBrush;
        }

        private static void ApplyQueryHighlightStyle(Run run)
        {
            run.FontWeight = FontWeights.SemiBold;
        }

        private static void ApplyQueryHighlightStyle(TextBlock textBlock)
        {
            textBlock.FontWeight = FontWeights.SemiBold;
        }

        private static TextSpan[] GetAnnotationSpans(string word)
        {
            return Regex.Matches(word, @"(\{.*?\})|(\[.*?\])|(\<.*?\>)")
                .Cast<Match>()
                .Select(match => new TextSpan(match.Index, match.Length))
                .ToArray();
        }
    }
}

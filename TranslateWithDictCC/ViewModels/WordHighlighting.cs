using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TranslateWithDictCC.Models;

namespace TranslateWithDictCC.ViewModels;

static partial class WordHighlighting
{
    static SolidColorBrush annotationBrush = null!;
    static SolidColorBrush queryHighlightBrush = null!;


    [GeneratedRegex(@"(\{.*?\})|(\[.*?\])|(\<.*?\>)")]
    private static partial Regex AnnotationsRegex();

    static WordHighlighting()
    {
        SetBrushes();

        Settings.Instance.PropertyChanged += Settings_PropertyChanged;
    }

    private static void SetBrushes()
    {
        ResourceDictionary dictionary = Settings.Instance.AppTheme switch
        {
            ElementTheme.Default => Application.Current.Resources,
            ElementTheme.Light => (ResourceDictionary)Application.Current.Resources.ThemeDictionaries["Light"],
            ElementTheme.Dark => (ResourceDictionary)Application.Current.Resources.ThemeDictionaries["Dark"],
            _ => Application.Current.Resources
        };

        annotationBrush = (SolidColorBrush)dictionary["DictionaryEntryAnnotationThemeBrush"];
        queryHighlightBrush = (SolidColorBrush)dictionary["DictionaryEntryQueryHighlightThemeBrush"];
    }

    private static void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.AppTheme))
            SetBrushes();
    }

    public static Block GenerateRichTextBlock(string word, TextSpan[] matchSpans, bool highlightQuery)
    {
        IReadOnlyList<TextSpan> annotationSpans = GetAnnotationSpans(word);

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

    private static IEnumerable<TextSpan> MergeSpans(TextSpan[] matchSpans, string word)
    {
        if (matchSpans.Length == 0)
            yield break;

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
                yield return currentSpan;
                currentSpan = matchSpans[i];
            }
        }

        yield return currentSpan;
    }

    private static void GenerateRun(Paragraph paragraph, string word, int offset, int length, IReadOnlyList<TextSpan> annotationSpans)
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

    private static void GenerateQueryHighlight(Paragraph paragraph, string word, IReadOnlyList<TextSpan> annotationSpans, TextSpan span)
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
        run.FontWeight = FontWeights.Medium;
    }

    private static void ApplyQueryHighlightStyle(TextBlock textBlock)
    {
        textBlock.FontWeight = FontWeights.Medium;
    }

    private static IReadOnlyList<TextSpan> GetAnnotationSpans(string word)
    {
        List<TextSpan> annotationSpans = [];

        foreach (var match in AnnotationsRegex().EnumerateMatches(word))
            annotationSpans.Add(new TextSpan(match.Index, match.Length));

        return annotationSpans;
    }

    public static string RemoveAnnotations(string word)
    {
        IReadOnlyList<TextSpan> annotationSpans = GetAnnotationSpans(word);

        StringBuilder wordWithoutAnnotations = new StringBuilder(word);

        foreach (TextSpan annotationSpan in annotationSpans.Reverse())
        {
            int offset = annotationSpan.Offset;
            int length = annotationSpan.Length;

            // also remove leading or trailing spaces (but not both at the same time)
            if (offset > 0 && wordWithoutAnnotations[offset - 1] == ' ')
            {
                offset--;
                length++;
            }
            else if (offset + length < wordWithoutAnnotations.Length && wordWithoutAnnotations[offset + length] == ' ')
                length++;

            wordWithoutAnnotations.Remove(offset, length);
        }

        return wordWithoutAnnotations.ToString().Trim();
    }
}

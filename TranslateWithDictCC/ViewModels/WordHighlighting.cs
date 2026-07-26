using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TranslateWithDictCC.Models;

namespace TranslateWithDictCC.ViewModels;

class FormattedWord
{
    public IReadOnlyList<FormattedWordFragment> Fragments { get; }

    public FormattedWord(IReadOnlyList<FormattedWordFragment> fragments)
    {
        Fragments = fragments;
    }
}

readonly record struct FormattedWordFragment(string Text, FormattedWordFragmentKind Kind);

enum FormattedWordFragmentKind
{
    Normal,
    Annotation,
    QueryHighlight,
    QueryHighlightAnnotation
}

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

    public static FormattedWord FormatWord(string word, TextSpan[] matchSpans, bool highlightQuery)
    {
        IReadOnlyList<TextSpan> annotationSpans = GetAnnotationSpans(word);
        List<FormattedWordFragment> fragments = [];

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
                    AddTextFragments(fragments, word, betweenOffset, betweenLength, annotationSpans);

                AddQueryHighlightFragment(fragments, word, annotationSpans, span);
                lastSpan = span;
            }
        }

        betweenOffset = lastSpan.Offset + lastSpan.Length;
        betweenLength = word.Length - betweenOffset;

        if (betweenLength != 0)
            AddTextFragments(fragments, word, betweenOffset, betweenLength, annotationSpans);

        return new FormattedWord(fragments);
    }

    public static void SetRichTextBlockContent(RichTextBlock richTextBlock, FormattedWord formattedWord)
    {
        if (ReferenceEquals(richTextBlock.Tag, formattedWord))
            return;

        richTextBlock.Tag = formattedWord;
        richTextBlock.Blocks.Clear();
        richTextBlock.Blocks.Add(CreateParagraph(formattedWord));
    }

    public static void ClearRichTextBlockContent(RichTextBlock richTextBlock)
    {
        richTextBlock.Tag = null;
        richTextBlock.Blocks.Clear();
    }

    private static Paragraph CreateParagraph(FormattedWord formattedWord)
    {
        Paragraph paragraph = new Paragraph();

        foreach (FormattedWordFragment fragment in formattedWord.Fragments)
            paragraph.Inlines.Add(CreateInline(fragment));

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

    private static Inline CreateInline(FormattedWordFragment fragment)
    {
        return fragment.Kind switch
        {
            FormattedWordFragmentKind.Normal => CreateRun(fragment.Text, annotation: false),
            FormattedWordFragmentKind.Annotation => CreateRun(fragment.Text, annotation: true),
            FormattedWordFragmentKind.QueryHighlight => CreateQueryHighlightInline(fragment.Text, annotation: false),
            FormattedWordFragmentKind.QueryHighlightAnnotation => CreateQueryHighlightInline(fragment.Text, annotation: true),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static void AddTextFragments(List<FormattedWordFragment> fragments, string word, int offset, int length, IReadOnlyList<TextSpan> annotationSpans)
    {
        int endOffset = offset + length;
        int currentOffset = offset;

        foreach (TextSpan annotationSpan in annotationSpans.Where(span => span.Intersects(new TextSpan(offset, length))))
        {
            int annotationOffset = Math.Max(annotationSpan.Offset, offset);
            int annotationLength = Math.Min(annotationSpan.Offset + annotationSpan.Length, endOffset) - annotationOffset;

            if (annotationOffset > currentOffset)
                AddFragment(fragments, word, currentOffset, annotationOffset - currentOffset, FormattedWordFragmentKind.Normal);

            AddFragment(fragments, word, annotationOffset, annotationLength, FormattedWordFragmentKind.Annotation);
            currentOffset = annotationOffset + annotationLength;
        }

        if (currentOffset < endOffset)
            AddFragment(fragments, word, currentOffset, endOffset - currentOffset, FormattedWordFragmentKind.Normal);
    }

    private static void AddQueryHighlightFragment(List<FormattedWordFragment> fragments, string word, IReadOnlyList<TextSpan> annotationSpans, TextSpan span)
    {
        bool annotation = annotationSpans.Any(annotationSpan => annotationSpan.Contains(span));
        AddFragment(fragments, word, span.Offset, span.Length, annotation ? FormattedWordFragmentKind.QueryHighlightAnnotation : FormattedWordFragmentKind.QueryHighlight);
    }

    private static void AddFragment(List<FormattedWordFragment> fragments, string word, int offset, int length, FormattedWordFragmentKind kind)
    {
        if (length == 0)
            return;

        string text = word.Substring(offset, length);

        if (fragments.Count != 0 && fragments[^1].Kind == kind)
        {
            FormattedWordFragment lastFragment = fragments[^1];
            fragments[^1] = new FormattedWordFragment(lastFragment.Text + text, kind);
        }
        else
            fragments.Add(new FormattedWordFragment(text, kind));
    }

    private static Run CreateRun(string text, bool annotation)
    {
        Run run = new Run() { Text = text };

        if (annotation)
            ApplyAnnotationStyle(run);
        else
            ApplyQueryHighlightStyle(run);

        return run;
    }

    private static InlineUIContainer CreateQueryHighlightInline(string text, bool annotation)
    {
        TextBlock textBlock = new TextBlock() { Text = text };

        if (annotation)
            ApplyAnnotationStyle(textBlock);
        else
            ApplyQueryHighlightStyle(textBlock);

        Border border = new Border()
        {
            Background = queryHighlightBrush,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(4, 0, 4, 0),
            Child = textBlock,
            Margin = new Thickness(0, -4, 0, 0),
            RenderTransform = new TranslateTransform() { Y = 4 }
        };

        return new InlineUIContainer() { Child = border };
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

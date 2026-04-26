using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TranslateWithDictCC.Models;

namespace TranslateWithDictCC;

partial class DictionaryEntryComparer : Comparer<DictionaryEntry>
{
    readonly string searchQuery;
    readonly bool reverseSearch;

    readonly Dictionary<string, MatchInfo> matchInfos = [];

    public DictionaryEntryComparer(string searchQuery, bool reverseSearch)
    {
        this.searchQuery = searchQuery;
        this.reverseSearch = reverseSearch;
    }

    public override int Compare(DictionaryEntry? x, DictionaryEntry? y)
    {
        if (ReferenceEquals(x, y))
            return 0;
        if (x is null)
            return -1;
        if (y is null)
            return 1;

        string searchResultX, searchResultY;

        if (!reverseSearch)
        {
            searchResultX = x.Word1;
            searchResultY = y.Word1;
        }
        else
        {
            searchResultX = x.Word2;
            searchResultY = y.Word2;
        }

        if (!matchInfos.TryGetValue(searchResultX, out MatchInfo? matchInfoX))
        {
            matchInfoX = new MatchInfo(searchQuery, searchResultX, x.MatchSpans!);
            matchInfos.Add(searchResultX, matchInfoX);
        }

        if (!matchInfos.TryGetValue(searchResultY, out MatchInfo? matchInfoY))
        {
            matchInfoY = new MatchInfo(searchQuery, searchResultY, y.MatchSpans!);
            matchInfos.Add(searchResultY, matchInfoY);
        }

        /*
         * no match in annotation -> [case-sensitivity] -> [number of additional words] -> [length of additional words * 2 + length of annotations] -> [alphabetically]
         * match in annotation    -> [case-sensitivity] -> [length of annotations] -> [alphabetical]
        */

        if (matchInfoX.IsMatchInAnnotation != matchInfoY.IsMatchInAnnotation)
            return matchInfoX.IsMatchInAnnotation.CompareTo(matchInfoY.IsMatchInAnnotation);

        if (!matchInfoX.IsMatchInAnnotation)
        {
            if (Settings.Instance.CaseSensitiveSearch)
                if (matchInfoX.IsCaseSensitiveMatch != matchInfoY.IsCaseSensitiveMatch)
                    return -matchInfoX.IsCaseSensitiveMatch.CompareTo(matchInfoY.IsCaseSensitiveMatch);

            if (matchInfoX.AdditionalWordCount != matchInfoY.AdditionalWordCount)
                return matchInfoX.AdditionalWordCount.CompareTo(matchInfoY.AdditionalWordCount);

            int weightedLengthX = matchInfoX.AdditionalWordsLength * 2 + matchInfoX.AnnotationLength;
            int weightedLengthY = matchInfoY.AdditionalWordsLength * 2 + matchInfoY.AnnotationLength;

            if (weightedLengthX != weightedLengthY)
                return weightedLengthX.CompareTo(weightedLengthY);

            return string.Compare(searchResultX, searchResultY, StringComparison.Ordinal);
        }
        else
        {
            if (Settings.Instance.CaseSensitiveSearch)
                if (matchInfoX.IsCaseSensitiveMatch != matchInfoY.IsCaseSensitiveMatch)
                    return -matchInfoX.IsCaseSensitiveMatch.CompareTo(matchInfoY.IsCaseSensitiveMatch);

            if (matchInfoX.AnnotationLength != matchInfoY.AnnotationLength)
                return matchInfoX.AnnotationLength.CompareTo(matchInfoY.AnnotationLength);

            return string.Compare(searchResultX, searchResultY, StringComparison.Ordinal);
        }
    }
}

internal partial class MatchInfo
{
    public bool IsCaseSensitiveMatch { get; }
    public int AnnotationLength { get; }
    public int AdditionalWordsLength { get; }
    public int AdditionalWordCount { get; }
    public bool IsMatchInAnnotation { get; }

    public MatchInfo(string searchQuery, string searchResult, TextSpan[] matchSpans)
    {
        string[] searchTokens = SearchTokensRegex().Matches(searchQuery).Select(m => m.Value).ToArray();

        IsCaseSensitiveMatch = matchSpans.Length == searchTokens.Length &&
            matchSpans
            .Zip(searchTokens, (span, token) => searchResult.AsSpan(span.Offset, span.Length).Equals(token, StringComparison.InvariantCulture))
            .All(result => result);

        TextSpan[] annotationSpans =
            AnnotationsRegex().Matches(searchResult)
            .Cast<Match>()
            .Select(match => new TextSpan(match.Index, match.Length))
            .ToArray();

        AnnotationLength = annotationSpans.Sum(span => span.Length);

        IsMatchInAnnotation = matchSpans.All(matchSpan => annotationSpans.Any(annotationSpan => annotationSpan.Contains(matchSpan)));

        if (!IsMatchInAnnotation)
        {
            bool currentlyInAdditionalWord = false;

            for (int i = 0; i < searchResult.Length; i++)
            {
                if (matchSpans.FirstOrDefault(span => i == span.Offset) is var matchSpan && matchSpan != default)
                {
                    i = matchSpan.Offset + matchSpan.Length - 1;
                    currentlyInAdditionalWord = false;
                    continue;
                }                    

                if (annotationSpans.FirstOrDefault(span => i == span.Offset) is var annotationSpan && annotationSpan != default)
                {
                    i = annotationSpan.Offset + annotationSpan.Length - 1;
                    currentlyInAdditionalWord = false;
                    continue;
                }

                if (char.IsLetterOrDigit(searchResult[i]))
                {
                    if (!currentlyInAdditionalWord)
                    {
                        AdditionalWordCount++;
                        currentlyInAdditionalWord = true;
                    }
                    AdditionalWordsLength++;
                }
                else
                {
                    currentlyInAdditionalWord = false;                    
                }
            }
        }
    }

    // Matches Unicode61 tokenizer of SQLite
    [GeneratedRegex(@"[\p{L}\p{N}\p{Co}]+")]
    private static partial Regex SearchTokensRegex();

    [GeneratedRegex(@"(\{.*?\})|(\[.*?\])|(\<.*?\>)", RegexOptions.ExplicitCapture)]
    private static partial Regex AnnotationsRegex();
}

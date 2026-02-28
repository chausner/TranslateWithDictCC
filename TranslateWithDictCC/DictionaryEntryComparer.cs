using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TranslateWithDictCC.Models;

namespace TranslateWithDictCC;

class DictionaryEntryComparer : Comparer<DictionaryEntry>
{
    string searchQuery;
    bool reverseSearch;

    Dictionary<string, MatchInfo> matchInfos = new Dictionary<string, MatchInfo>();

    public DictionaryEntryComparer(string searchQuery, bool reverseSearch)
    {
        this.searchQuery = searchQuery;
        this.reverseSearch = reverseSearch;
    }

    public override int Compare(DictionaryEntry x, DictionaryEntry y)
    {
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

        if (!matchInfos.TryGetValue(searchResultX, out MatchInfo matchInfoX))
        {
            matchInfoX = new MatchInfo(searchQuery, searchResultX, x.MatchSpans);
            matchInfos.Add(searchResultX, matchInfoX);
        }

        if (!matchInfos.TryGetValue(searchResultY, out MatchInfo matchInfoY))
        {
            matchInfoY = new MatchInfo(searchQuery, searchResultY, y.MatchSpans);
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

    private class MatchInfo
    {
        public bool IsCaseSensitiveMatch { get; }
        public int AnnotationLength { get; }
        public int AdditionalWordsLength { get; }
        public int AdditionalWordCount { get; }
        public bool IsMatchInAnnotation { get; }

        static readonly Regex annotationSpanRegex = new Regex(@"(\{.*?\})|(\[.*?\])|(\<.*?\>)", RegexOptions.ExplicitCapture);

        public MatchInfo(string searchQuery, string searchResult, TextSpan[] matchSpans)
        {
            IsCaseSensitiveMatch = matchSpans.Any(matchSpan => searchResult.AsSpan(matchSpan.Offset, matchSpan.Length).Equals(searchQuery, StringComparison.InvariantCulture));

            TextSpan[] annotationSpans =
                annotationSpanRegex.Matches(searchResult)
                .Cast<Match>()
                .Select(match => new TextSpan(match.Index, match.Length))
                .ToArray();

            AnnotationLength = annotationSpans.Sum(span => span.Length);

            IsMatchInAnnotation = matchSpans.All(matchSpan => annotationSpans.Any(annotationSpan => annotationSpan.Contains(matchSpan)));

            if (!IsMatchInAnnotation)
            {
                AdditionalWordsLength = searchResult.Length - AnnotationLength - searchQuery.Length;

                int lastSpaceIndex = -1;

                for (int i = 0; i < searchResult.Length; i++)
                {
                    if (matchSpans.Any(span => i >= span.Offset && i < span.Offset + span.Length))
                        continue;

                    if (annotationSpans.Any(span => i >= span.Offset && i < span.Offset + span.Length))
                        continue;

                    if (!char.IsLetterOrDigit(searchResult[i]))
                    {
                        if (lastSpaceIndex != i - 1)
                            AdditionalWordCount++;

                        lastSpaceIndex = i;
                    }
                }

                AdditionalWordCount++;
            }
        }
    }
}

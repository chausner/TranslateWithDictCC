using TranslateWithDictCC.Models;

namespace TranslateWithDictCC.Tests;

public class MatchInfoTests
{
    [Theory]
    [InlineData("word", "Word", false)]
    [InlineData("Word", "Word", true)]
    [InlineData("word", "word", true)]
    [InlineData("Multiple words", "Multiple Words", false)]
    [InlineData("Multiple Words", "Multiple Words", true)]
    [InlineData("Multiple words", "There are Multiple Words in the result", false)]
    [InlineData("Multiple Words", "There are Multiple Words in the result", true)]
    public void IsCaseSensitiveMatchTest(string searchQuery, string searchResult, bool isCaseSensitiveMatch)
    {
        // Arrange
        TextSpan[] matchSpans = GetMatchSpansForQueryTokens(searchQuery, searchResult);

        // Act
        MatchInfo matchInfo = new MatchInfo(searchQuery, searchResult, matchSpans);

        // Assert
        Assert.Equal(isCaseSensitiveMatch, matchInfo.IsCaseSensitiveMatch);
    }

    [Theory]
    [InlineData("sample", "sample", 0)]
    [InlineData("Multiple Words", "Multiple Words", 0)]
    [InlineData("Multiple Words", "There are Multiple Words in the result", 0)]
    [InlineData("sample", "This is a sample {n} [regional] <abbr>", 3 + 10 + 6)]
    [InlineData("Multiple Words", "Multiple Words <abbr>", 6)]
    [InlineData("Multiple Words abbr", "Multiple Words <abbr>", 6)]
    public void AnnotationLengthTest(string searchQuery, string searchResult, int annotationLength)
    {
        // Arrange
        TextSpan[] matchSpans = GetMatchSpansForQueryTokens(searchQuery, searchResult);

        // Act
        MatchInfo matchInfo = new MatchInfo(searchQuery, searchResult, matchSpans);

        // Assert
        Assert.Equal(annotationLength, matchInfo.AnnotationLength);
    }

    [Theory]
    [InlineData("regional", "base word [regional usage]", new[] { "regional" }, true)]
    [InlineData("base regional", "base [regional usage]", new[] { "base", "regional" }, false)]
    [InlineData("base", "base [regional usage]", new[] { "base" }, false)]
    public void IsMatchInAnnotationTest(string searchQuery, string searchResult, string[] matches, bool isMatchInAnnotation)
    {
        // Arrange
        TextSpan[] matchSpans = GetMatchSpans(searchResult, matches);

        // Act
        MatchInfo matchInfo = new MatchInfo(searchQuery, searchResult, matchSpans);

        // Assert
        Assert.Equal(isMatchInAnnotation, matchInfo.IsMatchInAnnotation);
    }

    [Theory]
    [InlineData("word", "word", new[] { "word" }, 0)]
    [InlineData("regional", "base word [regional usage]", new[] { "regional" }, 0)]
    [InlineData("base regional", "base [regional usage]", new[] { "base", "regional" }, 0)]
    [InlineData("base", "do a base jump", new[] { "base" }, 2 + 1 + 4)]
    [InlineData("multiple words", "multiple words", new[] { "multiple", "words" }, 0)]
    [InlineData("multiple words", "story with multiple words", new[] { "multiple", "words" }, 5 + 4)]
    public void AdditionalWordsLengthTest(string searchQuery, string searchResult, string[] matches, int additionalWordsLength)
    {
        // Arrange
        TextSpan[] matchSpans = GetMatchSpans(searchResult, matches);

        // Act
        MatchInfo matchInfo = new MatchInfo(searchQuery, searchResult, matchSpans);

        // Assert
        Assert.Equal(additionalWordsLength, matchInfo.AdditionalWordsLength);
    }

    [Theory]
    [InlineData("word", "word", new[] { "word" }, 0)]
    [InlineData("regional", "base word [regional usage]", new[] { "regional" }, 0)]
    [InlineData("base regional", "base [regional usage]", new[] { "base", "regional" }, 0)]
    [InlineData("base", "do a base jump", new[] { "base" }, 3)]
    [InlineData("multiple words", "multiple words", new[] { "multiple", "words" }, 0)]
    [InlineData("multiple words", "story with multiple words", new[] { "multiple", "words" }, 2)]
    [InlineData("multiple words", "multiple words story", new[] { "multiple", "words" }, 1)]
    public void AdditionalWordCountTest(string searchQuery, string searchResult, string[] matches, int additionalWordCount)
    {
        // Arrange
        TextSpan[] matchSpans = GetMatchSpans(searchResult, matches);

        // Act
        MatchInfo matchInfo = new MatchInfo(searchQuery, searchResult, matchSpans);

        // Assert
        Assert.Equal(additionalWordCount, matchInfo.AdditionalWordCount);
    }

    private static TextSpan[] GetMatchSpansForQueryTokens(string searchQuery, string searchResult)
    {
        string[] queryTokens = searchQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return GetMatchSpans(searchResult, queryTokens);
    }

    private static TextSpan[] GetMatchSpans(string searchResult, string[] matches)
    {
        List<TextSpan> matchSpans = [];

        int currentIndex = 0;

        foreach (string match in matches)
        {
            int matchIndex = searchResult.IndexOf(match, currentIndex, StringComparison.InvariantCultureIgnoreCase);
            if (matchIndex == -1)
                throw new ArgumentException($"Could not find '{match}' in '{searchResult}'.", nameof(matches));

            matchSpans.Add(new TextSpan(matchIndex, match.Length));
            currentIndex = matchIndex + match.Length;
        }

        return matchSpans.ToArray();
    }
}

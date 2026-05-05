using Microsoft.UI.Xaml;
using TranslateWithDictCC.Models;

namespace TranslateWithDictCC.ViewModels;

class SearchSuggestionViewModel : ViewModel
{
    readonly WordHighlighting wordHighlighting;

    public DictionaryEntry DictionaryEntry { get; }
    public SearchContext SearchContext { get; }

    public FormattedWord Word
    {
        get
        {
            if (field == null)
                Initialize();

            return field!;
        }

        private set;
    }

    public string WordText
    {
        get
        {
            return SearchContext.SelectedDirection.ReverseSearch ? DictionaryEntry.Word2 : DictionaryEntry.Word1;
        }
    }

    public SearchSuggestionViewModel(DictionaryEntry entry, SearchContext searchContext, WordHighlighting wordHighlighting)
    {
        DictionaryEntry = entry;
        SearchContext = searchContext;
        this.wordHighlighting = wordHighlighting;
    }

    private void Initialize()
    {
        bool reverseSearch = SearchContext.SelectedDirection.ReverseSearch;
        Word = wordHighlighting.FormatWord(reverseSearch ? DictionaryEntry.Word2 : DictionaryEntry.Word1, DictionaryEntry.MatchSpans!, false);
    }

    public Visibility GetWordClassVisibility(string wordClasses)
    {
        if (!string.IsNullOrEmpty(wordClasses))
            return Visibility.Visible;
        else
            return Visibility.Collapsed;
    }
}

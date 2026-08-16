using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using TranslateWithDictCC.Models;

namespace TranslateWithDictCC.ViewModels;

[WinRT.GeneratedBindableCustomProperty]
partial class SearchSuggestionViewModel : ObservableObject
{
    readonly WordHighlighter wordHighlighter;

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

    public SearchSuggestionViewModel(DictionaryEntry entry, SearchContext searchContext, WordHighlighter wordHighlighter)
    {
        DictionaryEntry = entry;
        SearchContext = searchContext;
        this.wordHighlighter = wordHighlighter;
    }

    private void Initialize()
    {
        bool reverseSearch = SearchContext.SelectedDirection.ReverseSearch;
        Word = wordHighlighter.FormatWord(reverseSearch ? DictionaryEntry.Word2 : DictionaryEntry.Word1, DictionaryEntry.MatchSpans!, false);
    }

    public Visibility GetWordClassVisibility(string wordClasses)
    {
        if (!string.IsNullOrEmpty(wordClasses))
            return Visibility.Visible;
        else
            return Visibility.Collapsed;
    }
}

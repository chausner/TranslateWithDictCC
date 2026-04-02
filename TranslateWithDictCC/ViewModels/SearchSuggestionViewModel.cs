using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using TranslateWithDictCC.Models;

namespace TranslateWithDictCC.ViewModels;

class SearchSuggestionViewModel : ViewModel
{
    public DictionaryEntry DictionaryEntry { get; }
    public SearchContext SearchContext { get; }

    public Block Word
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

    public SearchSuggestionViewModel(DictionaryEntry entry, SearchContext searchContext)
    {
        DictionaryEntry = entry;
        SearchContext = searchContext;
    }

    private void Initialize()
    {
        bool reverseSearch = SearchContext.SelectedDirection.ReverseSearch;
        Word = WordHighlighting.GenerateRichTextBlock(reverseSearch ? DictionaryEntry.Word2 : DictionaryEntry.Word1, DictionaryEntry.MatchSpans!, false);
    }

    public Visibility GetWordClassVisibility(string wordClasses)
    {
        if (!string.IsNullOrEmpty(wordClasses))
            return Visibility.Visible;
        else
            return Visibility.Collapsed;
    }
}

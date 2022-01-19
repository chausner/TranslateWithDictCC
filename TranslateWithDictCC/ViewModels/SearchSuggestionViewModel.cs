using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using TranslateWithDictCC.Models;

namespace TranslateWithDictCC.ViewModels
{
    class SearchSuggestionViewModel : ViewModel
    {
        public DictionaryEntry DictionaryEntry { get; }
        public SearchContext SearchContext { get; }

        Block word;

        public Block Word
        {
            get
            {
                if (word == null)
                    Initialize();

                return word;
            }
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
            word = WordHighlighting.GenerateRichTextBlock(reverseSearch ? DictionaryEntry.Word2 : DictionaryEntry.Word1, DictionaryEntry.MatchSpans, false);
        }

        public Visibility GetWordClassVisibility(string wordClasses)
        {
            if (!string.IsNullOrEmpty(wordClasses))
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }
    }
}

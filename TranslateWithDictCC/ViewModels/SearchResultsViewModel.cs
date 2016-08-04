using TranslateWithDictCC.Models;
using System.Collections.Generic;

namespace TranslateWithDictCC.ViewModels
{
    class SearchResultsViewModel
    {
        public IReadOnlyList<DictionaryEntryViewModel> DictionaryEntries { get; }
        public SearchContext SearchContext { get; }

        public SearchResultsViewModel(IList<DictionaryEntry> results, SearchContext searchContext)
        {
            SearchContext = searchContext;

            DictionaryEntries = new LazyCollection<DictionaryEntry, DictionaryEntryViewModel>(
                results, entry => new DictionaryEntryViewModel(entry, searchContext));

            for (int i = 0; i < 15 && i < results.Count; i++)
            {
                var tmp = DictionaryEntries[i];
            }
        }
    }
}

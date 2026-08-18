using System.Collections.Generic;

namespace TranslateWithDictCC.ViewModels;

class DictionaryEntryGroupViewModel
{
    public string GroupHeader { get; }
    public IReadOnlyList<DictionaryEntryViewModel> Entries { get; }

    public DictionaryEntryGroupViewModel(string groupHeader, IReadOnlyList<DictionaryEntryViewModel> entries)
    {
        GroupHeader = groupHeader;
        Entries = entries;
    }
}

using System.Collections.Generic;

namespace TranslateWithDictCC.ViewModels;

class DictionaryEntryGroupViewModel
{
    public string GroupHeader { get; }
    public IReadOnlyList<DictionaryEntryViewModel> Entries { get; }

    public DictionaryEntryGroupViewModel(string name, IReadOnlyList<DictionaryEntryViewModel> entries)
    {
        GroupHeader = name;
        Entries = entries;
    }
}

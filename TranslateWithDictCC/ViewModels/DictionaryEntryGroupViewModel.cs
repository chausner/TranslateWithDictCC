namespace TranslateWithDictCC.ViewModels;

[WinRT.GeneratedBindableCustomProperty]
partial class DictionaryEntryGroupViewModel
{
    public string GroupHeader { get; }
    public DictionaryEntryViewModel[] Entries { get; }

    public DictionaryEntryGroupViewModel(string groupHeader, DictionaryEntryViewModel[] entries)
    {
        GroupHeader = groupHeader;
        Entries = entries;
    }
}

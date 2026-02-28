namespace TranslateWithDictCC.ViewModels;

record SearchContext(
    string SearchQuery,
    DirectionViewModel SelectedDirection,
    bool DontSearchInBothDirections
);

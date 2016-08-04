namespace TranslateWithDictCC.ViewModels
{
    class SearchContext
    {
        public string SearchQuery { get; }
        public DirectionViewModel SelectedDirection { get; }

        public SearchContext(string searchQuery, DirectionViewModel selectedDirection)
        {
            SearchQuery = searchQuery;
            SelectedDirection = selectedDirection;
        }
    }
}

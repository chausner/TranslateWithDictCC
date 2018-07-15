using TranslateWithDictCC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.StartScreen;
using System.Collections.ObjectModel;

namespace TranslateWithDictCC.ViewModels
{
    class MainViewModel : ViewModel
    {
        public static readonly MainViewModel Instance = new MainViewModel();

        DirectionViewModel[] availableDirections;

        public DirectionViewModel[] AvailableDirections
        {
            get { return availableDirections; }
            set { SetProperty(ref availableDirections, value); }
        }

        DirectionViewModel selectedDirection;

        public DirectionViewModel SelectedDirection
        {
            get { return selectedDirection; }
            set { SetProperty(ref selectedDirection, value); }
        }

        bool isSearchInProgress;

        public bool IsSearchInProgress
        {
            get { return isSearchInProgress; }
            set { SetProperty(ref isSearchInProgress, value); }
        }

        public ObservableCollection<SearchSuggestionViewModel> SearchSuggestions { get; }

        public ICommand SwitchDirectionOfTranslationCommand { get; }
        public ICommand NavigateToPageCommand { get; set; }
        public ICommand GoBackToPageCommand { get; set; }

        SemaphoreSlim querySemaphore = new SemaphoreSlim(1);

        string lastQuery;

        private MainViewModel()
        {
            SearchSuggestions = new ObservableCollection<SearchSuggestionViewModel>();

            SwitchDirectionOfTranslationCommand = new RelayCommand(SwitchDirectionOfTranslation, CanSwitchDirectionOfTranslation);
        }

        public void LoadSettings()
        {
            Settings.Instance.GetSelectedDirection(out string originLanguageCode, out string destinationLanguageCode);

            SelectedDirection = AvailableDirections.FirstOrDefault(dvm =>
                dvm.OriginLanguageCode == originLanguageCode &&
                dvm.DestinationLanguageCode == destinationLanguageCode);

            if (SelectedDirection == null)
                SelectedDirection = AvailableDirections.FirstOrDefault();
        }

        public void SaveSettings()
        {
            Settings.Instance.SetSelectedDirection(SelectedDirection?.OriginLanguageCode, SelectedDirection?.DestinationLanguageCode);
        }

        public async Task UpdateDirection()
        {
            DirectionViewModel previouslySelected = SelectedDirection;
            
            SelectedDirection = null;

            AvailableDirections =
                (await DatabaseManager.Instance.GetDictionaries())
                .SelectMany(dict => new[] { new DirectionViewModel(dict, false), new DirectionViewModel(dict, true) })
                .OrderBy(dvm => dvm.OriginLanguage)
                .ToArray();

            SelectedDirection = null;

            if (previouslySelected != null)
                SelectedDirection = AvailableDirections.FirstOrDefault(dvm => dvm.Equals(previouslySelected));

            if (SelectedDirection == null)
                SelectedDirection = AvailableDirections.FirstOrDefault();

            await UpdateJumpList();
        }        

        private void SwitchDirectionOfTranslation()
        {
            if (SelectedDirection == null)
                return;

            SelectedDirection = AvailableDirections.First(dvm => dvm.EqualsReversed(selectedDirection));
        }

        private bool CanSwitchDirectionOfTranslation()
        {
            return SelectedDirection != null;
        }

        private async Task<List<DictionaryEntry>> PerformQueryInner(string searchQuery, bool dontSearchInBothDirections)
        {
            List<DictionaryEntry> results = await DatabaseManager.Instance.QueryEntries(selectedDirection.Dictionary, searchQuery, selectedDirection.ReverseSearch);

            if (results.Count == 0 && Settings.Instance.SearchInBothDirections && !dontSearchInBothDirections)
            {
                results = await DatabaseManager.Instance.QueryEntries(selectedDirection.Dictionary, searchQuery, !selectedDirection.ReverseSearch);

                if (results.Count != 0)
                    SwitchDirectionOfTranslation();
            }

            await Task.Run(delegate ()
            {
                results.Sort(new DictionaryEntryComparer(searchQuery, selectedDirection.ReverseSearch));
            });

            return results;
        }      

        public async Task PerformQuery(string searchQuery, bool dontSearchInBothDirections = false)
        {
            try
            {
                await querySemaphore.WaitAsync();

                lastQuery = searchQuery;

                // it may happen that the AutoSuggestBox does not hide search suggestions automatically after
                // performing a query. For these cases, clear the suggestions manually
                SearchSuggestions.Clear();

                Task<List<DictionaryEntry>> searchTask = PerformQueryInner(searchQuery, dontSearchInBothDirections);

                Task animationDelayTask = Task.Delay(250);

                Task finishedTask = await Task.WhenAny(searchTask, animationDelayTask);

                if (finishedTask == animationDelayTask)
                {
                    IsSearchInProgress = true;
                    await searchTask;
                }

                SearchContext searchContext = new SearchContext(searchQuery, selectedDirection);

                SearchResultsViewModel searchResultsViewModel = new SearchResultsViewModel(searchTask.Result, searchContext);

                // explicit type parameters required here
                NavigateToPageCommand.Execute(Tuple.Create<string, object>("SearchResultsPage", searchResultsViewModel));
            }
            finally
            {
                IsSearchInProgress = false;

                querySemaphore.Release();
            }            
        }

        private async Task UpdateJumpList()
        {
            if (!JumpList.IsSupported())
                return;

            try
            {
                JumpList jumpList = await JumpList.LoadCurrentAsync();

                jumpList.SystemGroupKind = JumpListSystemGroupKind.None;
                jumpList.Items.Clear();

                foreach (DirectionViewModel directionViewModel in AvailableDirections)
                {
                    string itemName = string.Format("{0} → {1}", directionViewModel.OriginLanguage, directionViewModel.DestinationLanguage);
                    string arguments = "dict:" + directionViewModel.OriginLanguageCode + directionViewModel.DestinationLanguageCode;

                    JumpListItem jumpListItem = JumpListItem.CreateWithArguments(arguments, itemName);

                    jumpListItem.Logo = LanguageCodes.GetCountryFlagUri(directionViewModel.OriginLanguageCode);

                    jumpList.Items.Add(jumpListItem);
                }
            
                await jumpList.SaveAsync();
            }
            catch
            {
                // in rare cases, SaveAsync may fail with HRESULT 0x80070497: "Unable to remove the file to be replaced."
                // this appears to be a common problem without a solution, so we simply ignore any errors here
            }
        }

        public async Task UpdateSearchSuggestions(string partialSearchQuery)
        {
            try
            {
                await querySemaphore.WaitAsync();

                // if the user types quickly and presses enter, the search suggestions may get triggered after the query
                // in this case, don't show the suggestions
                if (lastQuery == partialSearchQuery)
                {
                    SearchSuggestions.Clear();
                    return;
                }

                IList<SearchSuggestionViewModel> suggestions = await GetSearchSuggestions(partialSearchQuery);

                SearchSuggestions.Clear();

                if (suggestions != null)
                    foreach (SearchSuggestionViewModel suggestion in suggestions)
                        SearchSuggestions.Add(suggestion);

                lastQuery = null;
            }
            finally
            {
                querySemaphore.Release();
            }
        }

        private async Task<IList<SearchSuggestionViewModel>> GetSearchSuggestions(string partialSearchQuery)
        {
            const int maxResults = 1000;
            const int maxSuggestionsShown = 100;

            if (partialSearchQuery.Trim().Length < 3)
                return null;

            string searchQuery;

            if (!partialSearchQuery.EndsWith(" ") && partialSearchQuery != string.Empty)
                searchQuery = partialSearchQuery + "*";
            else
                searchQuery = partialSearchQuery;

            bool reverseSearch = SelectedDirection.ReverseSearch;

            List<DictionaryEntry> results = await DatabaseManager.Instance.QueryEntries(SelectedDirection.Dictionary, searchQuery, reverseSearch, maxResults + 1);

            if (results.Count == 0 && Settings.Instance.SearchInBothDirections)
            {
                reverseSearch = !reverseSearch;
                results = await DatabaseManager.Instance.QueryEntries(SelectedDirection.Dictionary, searchQuery, reverseSearch, maxResults + 1);
            }

            if (results.Count == 0 || results.Count > maxResults)
                return null;

            return await Task.Run(delegate ()
            {
                results.Sort(new DictionaryEntryComparer(searchQuery, reverseSearch));

                SearchContext searchContext = new SearchContext(searchQuery, new DirectionViewModel(SelectedDirection.Dictionary, reverseSearch));

                IEnumerable<SearchSuggestionViewModel> suggestions =
                    results
                    .DistinctBy(entry => reverseSearch ? entry.Word2 : entry.Word1)
                    .Select(entry => new SearchSuggestionViewModel(entry, searchContext))
                    .Take(maxSuggestionsShown);

                return suggestions.ToArray();
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TranslateWithDictCC.Models;
using Windows.UI.StartScreen;

namespace TranslateWithDictCC.ViewModels;

class SearchResultsViewModel : ViewModel
{
    public static readonly SearchResultsViewModel Instance = new SearchResultsViewModel();

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

    IReadOnlyList<DictionaryEntryViewModel> dictionaryEntries;

    public IReadOnlyList<DictionaryEntryViewModel> DictionaryEntries
    {
        get { return dictionaryEntries; }
        private set { SetProperty(ref dictionaryEntries, value); }
    }

    bool isSearchInProgress;

    public bool IsSearchInProgress
    {
        get { return isSearchInProgress; }
        set { SetProperty(ref isSearchInProgress, value); }
    }

    public ObservableCollection<SearchSuggestionViewModel> SearchSuggestions { get; }

		bool isOutdatedDictionariesInfoBarShown;

		public bool IsOutdatedDictionariesInfoBarShown
		{
			get { return isOutdatedDictionariesInfoBarShown; }
			set { SetProperty(ref isOutdatedDictionariesInfoBarShown, value); }
		}

		public ICommand SwitchDirectionOfTranslationCommand { get; }
		public ICommand GoToOptionsCommand { get; }

		SemaphoreSlim querySemaphore = new SemaphoreSlim(1);

    CancellationTokenSource searchSuggestionCancellationTokenSource;

    private SearchResultsViewModel()
    {
        SearchSuggestions = new ObservableCollection<SearchSuggestionViewModel>();

        SwitchDirectionOfTranslationCommand = new RelayCommand(SwitchDirectionOfTranslation, CanSwitchDirectionOfTranslation);
			GoToOptionsCommand = new RelayCommand(GoToOptions);

			SettingsViewModel.Instance.DictionariesChanged += SettingsViewModel_DictionariesChanged;
    }

    private async void SettingsViewModel_DictionariesChanged(object sender, EventArgs e)
    {
        await UpdateDirection();
    }

    public async Task Load()
    {
        await UpdateDirection();

        LoadSettings();

        bool hasOutdatedDictionaries = await DatabaseManager.Instance.HasOutdatedDictionaries();

        if (!Settings.Instance.OutdatedDictionariesNoticeRead)
        {
            IsOutdatedDictionariesInfoBarShown = hasOutdatedDictionaries;
            Settings.Instance.OutdatedDictionariesNoticeRead = hasOutdatedDictionaries;
        }
        else
        {
            IsOutdatedDictionariesInfoBarShown = false;
            if (!hasOutdatedDictionaries)
                Settings.Instance.OutdatedDictionariesNoticeRead = false;
			}
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

    private void GoToOptions()
    {
        MainViewModel.Instance.NavigateToPageCommand.Execute(Tuple.Create<string, object>("SettingsPage", null));
		}

    private async Task<List<DictionaryEntry>> PerformQueryInner(string searchQuery, bool dontSearchInBothDirections)
    {
        List<DictionaryEntry> results = await DatabaseManager.Instance.QueryEntries(selectedDirection.Dictionary, searchQuery, selectedDirection.ReverseSearch);

        if (results.Count == 0 && !dontSearchInBothDirections)
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
        if (searchSuggestionCancellationTokenSource != null)
            searchSuggestionCancellationTokenSource.Cancel();

        try
        {
            await querySemaphore.WaitAsync();

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

            SearchContext searchContext = new SearchContext(searchQuery, selectedDirection, dontSearchInBothDirections);

            List<DictionaryEntry> results = searchTask.Result;

            DictionaryEntries = new LazyCollection<DictionaryEntry, DictionaryEntryViewModel>(
                results, entry => new DictionaryEntryViewModel(entry, searchContext));
        }
        finally
        {
            IsSearchInProgress = false;

            querySemaphore.Release();
        }
    }

    private async Task UpdateSearchSuggestionsInner(string partialSearchQuery, CancellationToken cancellationToken)
    {
        IList<SearchSuggestionViewModel> suggestions;
        bool reverseSearch;

        try
        {
            await querySemaphore.WaitAsync(cancellationToken);

            (suggestions, reverseSearch) = await GetSearchSuggestions(partialSearchQuery, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            suggestions = null;
            reverseSearch = false;
        }
        finally
        {
            querySemaphore.Release();
        }

        if (suggestions != null)
            UpdateSearchSuggestions(suggestions, reverseSearch);
        else
            SearchSuggestions.Clear();
    }

    public Task UpdateSearchSuggestions(string partialSearchQuery)
    {
        if (searchSuggestionCancellationTokenSource != null)
            searchSuggestionCancellationTokenSource.Cancel();

        searchSuggestionCancellationTokenSource = new CancellationTokenSource();

        return UpdateSearchSuggestionsInner(partialSearchQuery, searchSuggestionCancellationTokenSource.Token);
    }

    private void UpdateSearchSuggestions(IList<SearchSuggestionViewModel> suggestions, bool reverseSearch)
    {
        SearchSuggestionViewModelComparer comparer = new SearchSuggestionViewModelComparer(reverseSearch, Settings.Instance.ShowWordClasses);

        for (int i = 0; i < SearchSuggestions.Count; i++)
            if (!suggestions.Contains(SearchSuggestions[i], comparer))
            {
                SearchSuggestions.RemoveAt(i);
                i--;
            }

        for (int i = 0; i < suggestions.Count; i++)
            if (!SearchSuggestions.Contains(suggestions[i], comparer))
                SearchSuggestions.Insert(i, suggestions[i]);
    }

    private class SearchSuggestionViewModelComparer : EqualityComparer<SearchSuggestionViewModel>
    {
        bool reverseSearch;
        bool compareWordClasses;

        public SearchSuggestionViewModelComparer(bool reverseSearch, bool compareWordClasses)
        {
            this.reverseSearch = reverseSearch;
            this.compareWordClasses = compareWordClasses;
        }

        public override bool Equals(SearchSuggestionViewModel x, SearchSuggestionViewModel y)
        {
            bool equal;

            if (reverseSearch)
                equal = x.DictionaryEntry.Word2 == y.DictionaryEntry.Word2;
            else
                equal = x.DictionaryEntry.Word1 == y.DictionaryEntry.Word1;

            if (compareWordClasses)
                equal &= x.DictionaryEntry.WordClasses == y.DictionaryEntry.WordClasses;

            return equal;
        }

        public override int GetHashCode(SearchSuggestionViewModel obj)
        {
            int hashCode = (reverseSearch ? obj.DictionaryEntry.Word2 : obj.DictionaryEntry.Word1).GetHashCode();

            if (compareWordClasses)
                hashCode ^= obj.DictionaryEntry.WordClasses.GetHashCode();

            return hashCode;
        }
    }

    private async Task<(IList<SearchSuggestionViewModel>, bool reverseSearch)> GetSearchSuggestions(string partialSearchQuery, CancellationToken cancellationToken)
    {
        const int maxResults = 1000;
        const int maxSuggestionsShown = 100;

        cancellationToken.ThrowIfCancellationRequested();

        if (partialSearchQuery.Trim().Length < 3)
            return (null, false);

        string searchQuery;

        if (!partialSearchQuery.EndsWith(" ") && partialSearchQuery != string.Empty)
            searchQuery = partialSearchQuery + "*";
        else
            searchQuery = partialSearchQuery;

        bool reverseSearch = SelectedDirection.ReverseSearch;

        List<DictionaryEntry> results = await DatabaseManager.Instance.QueryEntries(SelectedDirection.Dictionary, searchQuery, reverseSearch, maxResults + 1);

        cancellationToken.ThrowIfCancellationRequested();

        if (results.Count == 0)
        {
            reverseSearch = !reverseSearch;
            results = await DatabaseManager.Instance.QueryEntries(SelectedDirection.Dictionary, searchQuery, reverseSearch, maxResults + 1);
        }

        if (results.Count == 0 || results.Count > maxResults)
            return (null, false);

        cancellationToken.ThrowIfCancellationRequested();

        SearchSuggestionViewModel[] suggestions = await Task.Run(delegate ()
        {
            results.Sort(new DictionaryEntryComparer(searchQuery, reverseSearch));

            SearchContext searchContext = new SearchContext(searchQuery, new DirectionViewModel(SelectedDirection.Dictionary, reverseSearch), false);

            IEnumerable<SearchSuggestionViewModel> suggestions =
                results
                .DistinctBy(entry => reverseSearch ? entry.Word2 : entry.Word1)
                .Select(entry => new SearchSuggestionViewModel(entry, searchContext))
                .Take(maxSuggestionsShown);

            return suggestions.ToArray();
        }, cancellationToken);

        return (suggestions, reverseSearch);
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
}

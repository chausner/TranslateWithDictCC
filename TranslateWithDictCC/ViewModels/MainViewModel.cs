using TranslateWithDictCC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

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

        public ICommand SwitchDirectionOfTranslationCommand { get; }
        public ICommand NavigateToPageCommand { get; set; }
        public ICommand GoBackToPageCommand { get; set; }

        SemaphoreSlim querySemaphore = new SemaphoreSlim(1);

        private MainViewModel()
        {
            SwitchDirectionOfTranslationCommand = new RelayCommand(SwitchDirectionOfTranslation, CanSwitchDirectionOfTranslation);
        }

        public void LoadSettings()
        {
            string originLanguageCode;
            string destinationLanguageCode;

            Settings.Instance.GetSelectedDirection(out originLanguageCode, out destinationLanguageCode);

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
                .SelectMany(dict =>
                    new[] { new DirectionViewModel(dict, false), new DirectionViewModel(dict, true) })
                .OrderBy(dvm => dvm.OriginLanguage)
                .ToArray();

            SelectedDirection = null;

            if (previouslySelected != null)
                SelectedDirection = AvailableDirections.FirstOrDefault(dvm =>
                    dvm.OriginLanguageCode == previouslySelected.OriginLanguageCode &&
                    dvm.DestinationLanguageCode == previouslySelected.DestinationLanguageCode);

            if (SelectedDirection == null)
                SelectedDirection = AvailableDirections.FirstOrDefault();
        }        

        private void SwitchDirectionOfTranslation()
        {
            if (SelectedDirection == null)
                return;

            SelectedDirection = AvailableDirections.First(dvm => dvm.Dictionary == selectedDirection.Dictionary && 
                dvm.ReverseSearch == !selectedDirection.ReverseSearch);
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

                NavigateToPageCommand.Execute(new Tuple<string, object>("SearchResultsPage", searchResultsViewModel));
            }
            finally
            {
                IsSearchInProgress = false;

                querySemaphore.Release();
            }
        }
    }
}

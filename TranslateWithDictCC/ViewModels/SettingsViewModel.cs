using TranslateWithDictCC.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Globalization.DateTimeFormatting;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using System.Windows.Input;

namespace TranslateWithDictCC.ViewModels
{
    class SettingsViewModel : ViewModel
    {
        public static readonly SettingsViewModel Instance = new SettingsViewModel();

        public ObservableCollection<DictionaryViewModel> Dictionaries { get; }

        public ICommand ImportDictionaryCommand { get; }

        Task importQueueProcessTask;
        CancellationTokenSource cancellationTokenSource;

        ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

        private bool IsImportInProgress
        {
            get
            {
                return importQueueProcessTask != null && !importQueueProcessTask.IsCompleted;
            }
        }

        private SettingsViewModel()
        {
            Dictionaries = new ObservableCollection<DictionaryViewModel>();

            ImportDictionaryCommand = new RelayCommand(RunImportDictionaryCommand);
        }

        public async Task Load()
        {
            Dictionaries.Clear();

            foreach (Dictionary dictionary in await DatabaseManager.Instance.GetDictionaries())
                Dictionaries.Add(new DictionaryViewModel(dictionary));
        }

        private async void RunImportDictionaryCommand()
        {
            FileOpenPicker fileOpenPicker = new FileOpenPicker();

            fileOpenPicker.FileTypeFilter.Add(".txt");
            fileOpenPicker.FileTypeFilter.Add(".zip");

            IReadOnlyList<StorageFile> wordlistFiles = await fileOpenPicker.PickMultipleFilesAsync();

            if (wordlistFiles == null)
                return;

            foreach (StorageFile wordlistFile in wordlistFiles)
            {
                WordlistReader wordlistReader = new WordlistReader(wordlistFile);

                try
                {
                    await wordlistReader.ReadHeader();
                }
                catch
                {
                    wordlistReader.Dispose();

                    MessageDialog messageDialog = new MessageDialog(
                        string.Format(resourceLoader.GetString("Import_Header_Error_Body"), wordlistFile.Name),
                        resourceLoader.GetString("Import_Header_Error_Title"));

                    await messageDialog.ShowAsync();
                    continue;
                }

                bool allConflictsResolved = await CheckForConflictingDictionary(wordlistReader);

                if (allConflictsResolved)
                {
                    DictionaryViewModel dictionaryViewModel = new DictionaryViewModel(wordlistReader);

                    Dictionaries.Add(dictionaryViewModel);
                }
                else
                    wordlistReader.Dispose();
            }

            if (!IsImportInProgress)
            {
                cancellationTokenSource = new CancellationTokenSource();
                importQueueProcessTask = ProcessImportQueue();
            }
        }

        private async Task<bool> CheckForConflictingDictionary(WordlistReader wordlistReader)
        {
            DictionaryViewModel conflictingDictionary = Dictionaries.FirstOrDefault(dict =>
                 (dict.OriginLanguageCode == wordlistReader.OriginLanguageCode && dict.DestinationLanguageCode == wordlistReader.DestinationLanguageCode) ||
                 (dict.OriginLanguageCode == wordlistReader.DestinationLanguageCode && dict.DestinationLanguageCode == wordlistReader.OriginLanguageCode));

            if (conflictingDictionary == null)
                return true;

            DateTimeFormatter dateTimeFormatter = new DateTimeFormatter("shortdate shorttime");

            string content = resourceLoader.GetString("Import_Conflict_Body1");
            content += "\r\n\r\n";
            content += string.Format(resourceLoader.GetString("Import_Conflict_Body2"), 
                conflictingDictionary.OriginLanguage, conflictingDictionary.DestinationLanguage, dateTimeFormatter.Format(conflictingDictionary.CreationDate));
            content += "\r\n";
            content += string.Format(resourceLoader.GetString("Import_Conflict_Body3"), 
                conflictingDictionary.OriginLanguage, conflictingDictionary.DestinationLanguage, dateTimeFormatter.Format(wordlistReader.CreationDate));

            MessageDialog messageDialog = new MessageDialog(content, resourceLoader.GetString("Import_Conflict_Title"));

            messageDialog.Commands.Add(new UICommand(resourceLoader.GetString("Import_Conflict_Replace")));
            messageDialog.Commands.Add(new UICommand(resourceLoader.GetString("Import_Conflict_Skip")));

            messageDialog.DefaultCommandIndex = 0;
            messageDialog.CancelCommandIndex = 1;

            bool replace = await messageDialog.ShowAsync() == messageDialog.Commands[0];

            if (replace)
            {
                bool removedSuccessfully = await RemoveDictionary(conflictingDictionary, true);

                // as long as another import is still in process, we cannot replace a dictionary
                if (!removedSuccessfully)
                    return false;
            }

            return replace;
        }

        private async Task ProcessImportQueue()
        {
            while (true)
            {
                DictionaryViewModel dictionaryViewModel = Dictionaries.FirstOrDefault(dict => dict.Status == DictionaryStatus.Queued);

                if (dictionaryViewModel == null)
                    break;

                dictionaryViewModel.Status = DictionaryStatus.Installing;

                try
                {
                    await DatabaseManager.Instance.ImportWordlist(dictionaryViewModel, cancellationTokenSource.Token);

                    await DatabaseManager.Instance.OptimizeTable(dictionaryViewModel.Dictionary);
                }
                catch (OperationCanceledException)
                {
                    Dictionaries.Remove(dictionaryViewModel);
                    cancellationTokenSource = new CancellationTokenSource();
                    continue;
                }
                catch
                {
                    MessageDialog messageDialog = new MessageDialog(
                        string.Format(resourceLoader.GetString("Import_Error_Body"), dictionaryViewModel.OriginLanguage, dictionaryViewModel.DestinationLanguage), 
                        resourceLoader.GetString("Import_Error_Title"));

                    await messageDialog.ShowAsync();     

                    Dictionaries.Remove(dictionaryViewModel);
                    continue;
                }
                finally
                {
                    dictionaryViewModel.WordlistReader.Dispose();
                    dictionaryViewModel.WordlistReader = null;
                }

                dictionaryViewModel.Status = DictionaryStatus.Installed;

                await MainViewModel.Instance.UpdateDirection();
            }
        }

        public async Task<bool> RemoveDictionary(DictionaryViewModel dictionaryViewModel, bool noConfirmation)
        {
            if (dictionaryViewModel.Status == DictionaryStatus.Installed)
                if (IsImportInProgress)
                {
                    MessageDialog messageDialog = new MessageDialog(
                        resourceLoader.GetString("Import_In_Progress_Body"),
                        resourceLoader.GetString("Import_In_Progress_Title"));

                    await messageDialog.ShowAsync();
                    return false;
                }
                else if (!noConfirmation)
                {
                    MessageDialog messageDialog = new MessageDialog(string.Format(
                           resourceLoader.GetString("Remove_Dictionary_Confirmation_Body"),
                           dictionaryViewModel.OriginLanguage, dictionaryViewModel.DestinationLanguage));

                    messageDialog.Commands.Add(new UICommand(resourceLoader.GetString("Remove_Dictionary_Confirmation_Remove")));
                    messageDialog.Commands.Add(new UICommand(resourceLoader.GetString("Remove_Dictionary_Confirmation_Cancel")));

                    messageDialog.DefaultCommandIndex = 0;
                    messageDialog.CancelCommandIndex = 1;

                    if (await messageDialog.ShowAsync() != messageDialog.Commands[0])
                        return false;
                }

            switch (dictionaryViewModel.Status)
            {
                case DictionaryStatus.Queued:
                    Dictionaries.Remove(dictionaryViewModel);
                    break;
                case DictionaryStatus.Installing:
                    cancellationTokenSource.Cancel();
                    break;
                case DictionaryStatus.Installed:
                    await DatabaseManager.Instance.DeleteDictionary(dictionaryViewModel.Dictionary);
                    Dictionaries.Remove(dictionaryViewModel);
                    await MainViewModel.Instance.UpdateDirection();
                    break;
            }

            return true;
        }
    }
}

﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TranslateWithDictCC.Models;
using Windows.Globalization.DateTimeFormatting;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace TranslateWithDictCC.ViewModels
{
    class SettingsViewModel : ViewModel
    {
        public static readonly SettingsViewModel Instance = new SettingsViewModel();

        public ObservableCollection<DictionaryViewModel> Dictionaries { get; }

        Visibility restartAppTextBlockVisibility;

        public Visibility RestartAppTextBlockVisibility
        {
            get
            {
                return restartAppTextBlockVisibility;
            }
            private set
            {
                SetProperty(ref restartAppTextBlockVisibility, value);
            }
        }

        Visibility outdatedDictionariesInfoBarVisibility;

        public Visibility OutdatedDictionariesInfoBarVisibility
        {
            get
            {
                return outdatedDictionariesInfoBarVisibility;
            }
            private set
            {
                SetProperty(ref outdatedDictionariesInfoBarVisibility, value);
            }
        }

        public ICommand ImportDictionaryCommand { get; }

        Task importQueueProcessTask;
        CancellationTokenSource cancellationTokenSource;

        ResourceLoader resourceLoader = new ResourceLoader();

        private bool IsImportInProgress
        {
            get
            {
                return importQueueProcessTask != null && !importQueueProcessTask.IsCompleted;
            }
        }

        public event EventHandler<EventArgs> DictionariesChanged;

        ElementTheme appThemeAtStartup;

        private SettingsViewModel()
        {
            Dictionaries = new ObservableCollection<DictionaryViewModel>();

            RestartAppTextBlockVisibility = Visibility.Collapsed;
            OutdatedDictionariesInfoBarVisibility = Visibility.Collapsed;

            DictionariesChanged += SettingsViewModel_DictionariesChanged;

            ImportDictionaryCommand = new RelayCommand(RunImportDictionaryCommand);

            appThemeAtStartup = Settings.Instance.AppTheme;

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
        }

        private async void SettingsViewModel_DictionariesChanged(object sender, EventArgs e)
        {
            bool hasOutdatedDictionaries = await DatabaseManager.Instance.HasOutdatedDictionaries();
            OutdatedDictionariesInfoBarVisibility = hasOutdatedDictionaries ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.AppTheme))
                if (Settings.Instance.AppTheme != appThemeAtStartup)
                    RestartAppTextBlockVisibility = Visibility.Visible;
                else
                    RestartAppTextBlockVisibility = Visibility.Collapsed;
        }

        public async Task Load()
        {
            Dictionaries.Clear();

            foreach (Dictionary dictionary in await DatabaseManager.Instance.GetDictionaries())
                Dictionaries.Add(new DictionaryViewModel(dictionary));

            DictionariesChanged?.Invoke(this, EventArgs.Empty);
        }

        private async void RunImportDictionaryCommand()
        {
            FileOpenPicker fileOpenPicker = new FileOpenPicker();

            fileOpenPicker.FileTypeFilter.Add(".txt");
            fileOpenPicker.FileTypeFilter.Add(".zip");

            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.Instance);
            WinRT.Interop.InitializeWithWindow.Initialize(fileOpenPicker, hwnd);

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

                    ContentDialog contentDialog = new ContentDialog()
                    {
                        Title = resourceLoader.GetString("Import_Header_Error_Title"),
                        Content = string.Format(resourceLoader.GetString("Import_Header_Error_Body"), wordlistFile.Name),
                        CloseButtonText = "OK",
                        XamlRoot = MainWindow.Instance.Content.XamlRoot
                    };

                    await contentDialog.ShowAsync();
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

            ContentDialog contentDialog = new ContentDialog()
            {
                Title = resourceLoader.GetString("Import_Conflict_Title"),
                Content = content,
                PrimaryButtonText = resourceLoader.GetString("Import_Conflict_Replace"),
                CloseButtonText = resourceLoader.GetString("Import_Conflict_Skip"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = MainWindow.Instance.Content.XamlRoot
            };

            bool replace = await contentDialog.ShowAsync() == ContentDialogResult.Primary;

            if (replace)
            {
                bool removedSuccessfully = await RemoveDictionary(conflictingDictionary);

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

                Progress<WordlistImportProgress> progress = new Progress<WordlistImportProgress>(p =>
                {
                    dictionaryViewModel.NumberOfEntries = p.NumberOfEntriesImported;
                    dictionaryViewModel.ImportProgress = p.Progress;
                });

                try
                {
                    // run on the thread pool for better UI responsiveness
                    await Task.Run(async delegate ()
                    {
						dictionaryViewModel.Dictionary = await DatabaseManager.Instance.ImportWordlist(dictionaryViewModel.WordlistReader, progress, cancellationTokenSource.Token);

						await DatabaseManager.Instance.OptimizeTable(dictionaryViewModel.Dictionary);
                    });
                }
                catch (OperationCanceledException)
                {
                    Dictionaries.Remove(dictionaryViewModel);
                    cancellationTokenSource = new CancellationTokenSource();
                    continue;
                }
                catch
                {
                    ContentDialog contentDialog = new ContentDialog()
                    {
                        Title = resourceLoader.GetString("Import_Error_Title"),
                        Content = string.Format(resourceLoader.GetString("Import_Error_Body"), dictionaryViewModel.OriginLanguage, dictionaryViewModel.DestinationLanguage),
                        CloseButtonText = "OK",
                        XamlRoot = MainWindow.Instance.Content.XamlRoot
                    };

                    await contentDialog.ShowAsync();

                    Dictionaries.Remove(dictionaryViewModel);
                    continue;
                }
                finally
                {
                    dictionaryViewModel.WordlistReader.Dispose();
                    dictionaryViewModel.WordlistReader = null;
                }

                dictionaryViewModel.Status = DictionaryStatus.Installed;

                DictionariesChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task<bool> RemoveDictionary(DictionaryViewModel dictionaryViewModel)
        {
            if (dictionaryViewModel.Status != DictionaryStatus.Installed)
                return false;

            if (IsImportInProgress)
            {
                ContentDialog contentDialog = new ContentDialog()
                {
                    Title = resourceLoader.GetString("Import_In_Progress_Title"),
                    Content = resourceLoader.GetString("Import_In_Progress_Body"),
                    CloseButtonText = "OK",
                    XamlRoot = MainWindow.Instance.Content.XamlRoot
                };

                await contentDialog.ShowAsync();
                return false;
            }

            // run on the thread pool for better UI responsiveness
            await Task.Run(async delegate ()
            {
                await DatabaseManager.Instance.DeleteDictionary(dictionaryViewModel.Dictionary);
            });

            Dictionaries.Remove(dictionaryViewModel);

            DictionariesChanged?.Invoke(this, EventArgs.Empty);

            return true;
        }

        public void AbortImport(DictionaryViewModel dictionaryViewModel)
        {
            switch (dictionaryViewModel.Status)
            {
                case DictionaryStatus.Queued:
                    Dictionaries.Remove(dictionaryViewModel);
                    break;
                case DictionaryStatus.Installing:
                    cancellationTokenSource.Cancel();
                    break;
            }
        }
    }
}

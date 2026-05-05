using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TranslateWithDictCC.Models;
using TranslateWithDictCC.Services;
using Windows.Globalization.DateTimeFormatting;

namespace TranslateWithDictCC.ViewModels;

class SettingsViewModel : ViewModel
{
    readonly DatabaseManager databaseManager;
    readonly Settings settings;
    readonly DialogService dialogService;

    public ObservableCollection<DictionaryViewModel> Dictionaries { get; }

    public Visibility RestartAppTextBlockVisibility
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public Visibility OutdatedDictionariesInfoBarVisibility
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public ICommand ImportDictionaryCommand { get; }

    Task? importQueueProcessTask;
    CancellationTokenSource? cancellationTokenSource;

    readonly ResourceLoader resourceLoader = new ResourceLoader();

    private bool IsImportInProgress
    {
        get
        {
            return importQueueProcessTask != null && !importQueueProcessTask.IsCompleted;
        }
    }

    public event EventHandler<EventArgs>? DictionariesChanged;

    readonly ElementTheme appThemeAtStartup;

    public SettingsViewModel(DatabaseManager databaseManager, Settings settings, DialogService dialogService)
    {
        this.databaseManager = databaseManager;
        this.settings = settings;
        this.dialogService = dialogService;

        Dictionaries = new ObservableCollection<DictionaryViewModel>();

        RestartAppTextBlockVisibility = Visibility.Collapsed;
        OutdatedDictionariesInfoBarVisibility = Visibility.Collapsed;

        DictionariesChanged += SettingsViewModel_DictionariesChanged;

        ImportDictionaryCommand = new RelayCommand(RunImportDictionaryCommand);

        appThemeAtStartup = settings.AppTheme;

        settings.PropertyChanged += Settings_PropertyChanged;
    }

    private async void SettingsViewModel_DictionariesChanged(object? sender, EventArgs e)
    {
        bool hasOutdatedDictionaries = await databaseManager.HasOutdatedDictionaries();
        OutdatedDictionariesInfoBarVisibility = hasOutdatedDictionaries ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.AppTheme))
            if (settings.AppTheme != appThemeAtStartup)
                RestartAppTextBlockVisibility = Visibility.Visible;
            else
                RestartAppTextBlockVisibility = Visibility.Collapsed;
    }

    public async Task Load()
    {
        Dictionaries.Clear();

        foreach (Dictionary dictionary in await databaseManager.GetDictionaries())
            Dictionaries.Add(new DictionaryViewModel(dictionary, this));

        DictionariesChanged?.Invoke(this, EventArgs.Empty);
    }

    private async void RunImportDictionaryCommand()
    {
        FileOpenPicker fileOpenPicker = new FileOpenPicker(MainWindow.Instance.AppWindow.Id);

        fileOpenPicker.FileTypeFilter.Add(".txt");
        fileOpenPicker.FileTypeFilter.Add(".zip");

        string[] paths = 
            (await fileOpenPicker.PickMultipleFilesAsync())
            .Select(pickFileResult => pickFileResult.Path)
            .ToArray();

        if (paths.Length == 0)
            return;

        foreach (string path in paths)
        {
            WordlistReader wordlistReader = new WordlistReader(path);

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
                    Content = string.Format(resourceLoader.GetString("Import_Header_Error_Body"), path),
                    CloseButtonText = "OK"
                };

                await dialogService.ShowDialogAsync(contentDialog);
                continue;
            }

            bool allConflictsResolved = await CheckForConflictingDictionary(wordlistReader);

            if (allConflictsResolved)
            {
                DictionaryViewModel dictionaryViewModel = new DictionaryViewModel(wordlistReader, this);

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
        DictionaryViewModel? conflictingDictionary = Dictionaries.FirstOrDefault(dict =>
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
            DefaultButton = ContentDialogButton.Primary
        };

        bool replace = await dialogService.ShowDialogAsync(contentDialog) == ContentDialogResult.Primary;

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
            DictionaryViewModel? dictionaryViewModel = Dictionaries.FirstOrDefault(dict => dict.Status == DictionaryStatus.Queued);

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
                    dictionaryViewModel.Dictionary = await databaseManager.ImportWordlist(dictionaryViewModel.WordlistReader!, progress, cancellationTokenSource!.Token);

                    await databaseManager.OptimizeTable(dictionaryViewModel.Dictionary!);
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
                    CloseButtonText = "OK"
                };

                await dialogService.ShowDialogAsync(contentDialog);

                Dictionaries.Remove(dictionaryViewModel);
                continue;
            }
            finally
            {
                dictionaryViewModel.WordlistReader!.Dispose();
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
                CloseButtonText = "OK"
            };

            await dialogService.ShowDialogAsync(contentDialog);
            return false;
        }

        // run on the thread pool for better UI responsiveness
        await Task.Run(async delegate ()
        {
            await databaseManager.DeleteDictionary(dictionaryViewModel.Dictionary!);
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
                cancellationTokenSource!.Cancel();
                break;
        }
    }
}

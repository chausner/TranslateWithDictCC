using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using TranslateWithDictCC.Models;
using TranslateWithDictCC.Services;

namespace TranslateWithDictCC.ViewModels;

partial class DictionaryEntryViewModel : ViewModel
{
    readonly SearchResultsViewModel searchResultsViewModel;
    readonly Settings settings;
    readonly SubjectInfo subjectInfo;
    readonly AudioPlayer audioPlayer;
    readonly WordHighlighting wordHighlighting;
    readonly NavigationService navigationService;
    readonly DialogService dialogService;

    public DictionaryEntry DictionaryEntry { get; }
    public SearchContext SearchContext { get; }

    public AudioRecordingState AudioRecordingState1
    {
        get;
        set => SetProperty(ref field, value);
    }

    public AudioRecordingState AudioRecordingState2
    {
        get;
        set => SetProperty(ref field, value);
    }

    List<DictionaryEntryAttribute>? attributes;

    public FormattedWord Word1
    {
        get
        {
            if (field == null)
                Initialize();

            return field!;
        }

        private set;
    }

    public FormattedWord Word2
    {
        get
        {
            if (field == null)
                Initialize();

            return field!;
        }

        private set;
    }

    public IReadOnlyList<DictionaryEntryAttribute> Attributes
    {
        get
        {
            if (attributes == null)
                Initialize();

            return attributes!;
        }
    }

    public ICommand PlayStopAudioRecording1Command { get; }
    public ICommand PlayStopAudioRecording2Command { get; }
    public ICommand Search1Command { get; }
    public ICommand Search2Command { get; }

    [GeneratedRegex(@"\[([^\[\]]+)\]")]
    private static partial Regex SubjectsRegex();

    public DictionaryEntryViewModel(
        DictionaryEntry entry,
        SearchContext searchContext,
        SearchResultsViewModel searchResultsViewModel,
        Settings settings,
        SubjectInfo subjectInfo,
        AudioPlayer audioPlayer,
        WordHighlighting wordHighlighting,
        NavigationService navigationService,
        DialogService dialogService)
    {
        DictionaryEntry = entry;
        SearchContext = searchContext;
        this.searchResultsViewModel = searchResultsViewModel;
        this.settings = settings;
        this.subjectInfo = subjectInfo;
        this.audioPlayer = audioPlayer;
        this.wordHighlighting = wordHighlighting;
        this.navigationService = navigationService;
        this.dialogService = dialogService;
        AudioRecordingState1 = AudioRecordingState.Available;
        AudioRecordingState2 = AudioRecordingState.Available;
        PlayStopAudioRecording1Command = new RelayCommand(() => { PlayStopAudioRecording(false); });
        PlayStopAudioRecording2Command = new RelayCommand(() => { PlayStopAudioRecording(true); });
        Search1Command = new RelayCommand(() => { Search(false); });
        Search2Command = new RelayCommand(() => { Search(true); });
    }

    private void Initialize()
    {
        bool reverseSearch = SearchContext.SelectedDirection.ReverseSearch;
        Word1 = wordHighlighting.FormatWord(reverseSearch ? DictionaryEntry.Word2 : DictionaryEntry.Word1, DictionaryEntry.MatchSpans!, true);
        Word2 = wordHighlighting.FormatWord(reverseSearch ? DictionaryEntry.Word1 : DictionaryEntry.Word2, DictionaryEntry.MatchSpans!, false);

        attributes = [];

        if (settings.ShowWordClasses && DictionaryEntry.WordClasses != null)
            attributes.Add(new DictionaryEntryAttribute(DictionaryEntry.WordClasses, null));

        if (settings.ShowSubjects && DictionaryEntry.Subjects != null && subjectInfo.IsLoaded)
        {
            var subjectMatches = SubjectsRegex().EnumerateMatches(DictionaryEntry.Subjects);

            foreach (ValueMatch subjectMatch in subjectMatches)
            {
                string subjectString = DictionaryEntry.Subjects.Substring(subjectMatch.Index + 1, subjectMatch.Length - 2);

                var subject = subjectInfo.LookupSubject(SearchContext.SelectedDirection.OriginLanguageCode, SearchContext.SelectedDirection.DestinationLanguageCode, subjectString);

                if (subject != null)
                    attributes.Add(new DictionaryEntryAttribute(subject.Value.LocalizedSubject, subject.Value.Description));
            }
        }
    }

    public IconElement GetMoreButtonIcon(AudioRecordingState state)
    {
        Symbol symbol = state switch
        {
            AudioRecordingState.Available => Symbol.More,
            AudioRecordingState.Playing => Symbol.Pause, // Symbol.Stop
            AudioRecordingState.Unavailable => Symbol.More,
            _ => throw new ArgumentException()
        };

        return new IconSourceElement() { IconSource = new SymbolIconSource() { Symbol = symbol } };
    }

    public Visibility GetWordClassVisibility(string wordClasses)
    {
        if (!string.IsNullOrEmpty(wordClasses))
            return Visibility.Visible;
        else
            return Visibility.Collapsed;
    }

    private async void PlayStopAudioRecording(bool word2)
    {
        switch (word2 ? AudioRecordingState2 : AudioRecordingState1)
        {
            case AudioRecordingState.Available:
                await audioPlayer.PlayAudioRecording(this, word2);
                break;
            case AudioRecordingState.Playing:
                if (audioPlayer.CurrentlyPlayingAudioRecording == this)
                    audioPlayer.Pause();
                if (word2)
                    AudioRecordingState2 = AudioRecordingState.Available;
                else
                    AudioRecordingState1 = AudioRecordingState.Available;
                break;
            case AudioRecordingState.Unavailable:
                await ShowAudioRecordingNotAvailableMessage();
                break;
        }
    }

    private async Task ShowAudioRecordingNotAvailableMessage()
    {
        ResourceLoader resourceLoader = new ResourceLoader();

        ContentDialog contentDialog = new ContentDialog()
        {
            Title = resourceLoader.GetString("Audio_Recording_Not_Available_Title"),
            Content = resourceLoader.GetString("Audio_Recording_Not_Available_Body"),
            CloseButtonText = "OK"
        };

        await dialogService.ShowDialogAsync(contentDialog);
    }

    private void Search(bool word2)
    {
        string word = (word2 ^ SearchContext.SelectedDirection.ReverseSearch) ? DictionaryEntry.Word2 : DictionaryEntry.Word1;

        string searchTerm = WordHighlighting.RemoveAnnotations(word);

        if (word2)
            searchResultsViewModel.SwitchDirectionOfTranslationCommand.Execute(null);

        SearchContext searchContext = new SearchContext(searchTerm, searchResultsViewModel.SelectedDirection!, true);

        navigationService.NavigateToSearchResultsPage(searchContext);
    }
}

record DictionaryEntryAttribute(string Text, string? ToolTipText);

enum AudioRecordingState
{
    Available,
    Playing,
    Unavailable
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using TranslateWithDictCC.Models;
using Windows.UI;

namespace TranslateWithDictCC.ViewModels
{
    class DictionaryEntryViewModel : ViewModel
    {
        public DictionaryEntry DictionaryEntry { get; }
        public SearchContext SearchContext { get; }

        AudioRecordingState audioRecordingState1;

        public AudioRecordingState AudioRecordingState1
        {
            get { return audioRecordingState1; }
            set { SetProperty(ref audioRecordingState1, value); }
        }

        AudioRecordingState audioRecordingState2;

        public AudioRecordingState AudioRecordingState2
        {
            get { return audioRecordingState2; }
            set { SetProperty(ref audioRecordingState2, value); }
        }

        Block word1;
        Block word2;
        List<UIElement> subjects;

        public Block Word1
        {
            get
            {
                if (word1 == null)
                    Initialize();

                return word1;
            }
        }

        public Block Word2
        {
            get
            {
                if (word2 == null)
                    Initialize();

                return word2;
            }
        }

        public List<UIElement> Subjects
        {
            get
            {
                if (subjects == null)
                    Initialize();

                return subjects;
            }
        }

        public ICommand PlayStopAudioRecording1Command { get; }
        public ICommand PlayStopAudioRecording2Command { get; }

        public DictionaryEntryViewModel(DictionaryEntry entry, SearchContext searchContext)
        {
            DictionaryEntry = entry;
            SearchContext = searchContext;
            AudioRecordingState1 = AudioRecordingState.Available;
            AudioRecordingState2 = AudioRecordingState.Available;
            PlayStopAudioRecording1Command = new RelayCommand(() => { PlayStopAudioRecording(false); });
            PlayStopAudioRecording2Command = new RelayCommand(() => { PlayStopAudioRecording(true); });
        }

        private void Initialize()
        {
            bool reverseSearch = SearchContext.SelectedDirection.ReverseSearch;
            word1 = WordHighlighting.GenerateRichTextBlock(reverseSearch ? DictionaryEntry.Word2 : DictionaryEntry.Word1, DictionaryEntry.MatchSpans, true);
            word2 = WordHighlighting.GenerateRichTextBlock(reverseSearch ? DictionaryEntry.Word1 : DictionaryEntry.Word2, DictionaryEntry.MatchSpans, false);

            subjects = new List<UIElement>();

            if (DictionaryEntry.Subjects != null)
            {
                IEnumerable<string> subjectStrings =
                    Regex.Matches(DictionaryEntry.Subjects, @"\[([^\[\]]+)\]")
                    .Cast<Match>()
                    .Select(match => match.Groups[1].Value);

                SubjectInfo.LoadAsync();

                foreach (string subjectString in subjectStrings)
                {
                    string description = SubjectInfo.GetSubjectDescription(SearchContext.SelectedDirection.OriginLanguageCode, SearchContext.SelectedDirection.DestinationLanguageCode, subjectString);

                    if (description != null)
                    {
                        Border border = new Border();
                        border.Padding = new Thickness(5, 2, 5, 2);
                        border.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xDA, 0xDA, 0xDA));
                        if (subjects.Count != 0)
                            border.Margin = new Thickness(5, 0, 0, 0);
                        border.Child = new TextBlock() { Text = subjectString };
                        ToolTip toolTip = new ToolTip();
                        toolTip.Content = description;
                        ToolTipService.SetToolTip(border, toolTip);
                        subjects.Add(border);
                    }
                }
            }
        }

        public IconElement GetAudioRecordingButtonIcon(AudioRecordingState state)
        {
            return state switch
            {
                AudioRecordingState.Available => new IconSourceElement() { IconSource = new SymbolIconSource() { Symbol = Symbol.Volume } },
                AudioRecordingState.Playing => new IconSourceElement() { IconSource = new SymbolIconSource() { Symbol = Symbol.Pause } }, // Symbol.Stop
                AudioRecordingState.Unavailable => new IconSourceElement() { IconSource = new SymbolIconSource() { Symbol = Symbol.Mute } },
                _ => throw new ArgumentException()
            };
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
                    await AudioPlayer.Instance.PlayAudioRecording(this, word2);
                    break;
                case AudioRecordingState.Playing:
                    if (AudioPlayer.Instance.CurrentlyPlayingAudioRecording == this)
                        AudioPlayer.Instance.Pause();
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
                CloseButtonText = "OK",
                XamlRoot = MainWindow.Instance.Content.XamlRoot
            };

            await contentDialog.ShowAsync();
        }
    }

    enum AudioRecordingState
    {
        Available,
        Playing,
        Unavailable
    }
}

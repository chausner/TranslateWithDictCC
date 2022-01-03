using TranslateWithDictCC.Models;
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;

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

        public DictionaryEntryViewModel(DictionaryEntry entry, SearchContext searchContext)
        {
            DictionaryEntry = entry;
            SearchContext = searchContext;
            AudioRecordingState1 = AudioRecordingState.Available;
            AudioRecordingState2 = AudioRecordingState.Available;
        }

        private void Initialize()
        {
            bool reverseSearch = SearchContext.SelectedDirection.ReverseSearch;
            word1 = WordHighlighting.GenerateRichTextBlock(reverseSearch ? DictionaryEntry.Word2 : DictionaryEntry.Word1, SearchContext.SearchQuery, DictionaryEntry.MatchSpans, true);
            word2 = WordHighlighting.GenerateRichTextBlock(reverseSearch ? DictionaryEntry.Word1 : DictionaryEntry.Word2, SearchContext.SearchQuery, DictionaryEntry.MatchSpans, false);
        }        

        public char GetAudioRecordingButtonText(AudioRecordingState state)
        {
            return state switch
            {
                AudioRecordingState.Available => (char)0xE767,
                AudioRecordingState.Playing => (char)0xE769,// 0xE71A
                AudioRecordingState.Unavailable => (char)0xE74F,
                _ => throw new ArgumentException(),
            };
        }

        public Visibility GetWordClassVisibility(string wordClasses)
        {
            if (!string.IsNullOrEmpty(wordClasses))
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }
    }

    enum AudioRecordingState
    {
        Available,
        Playing,
        Unavailable
    }
}

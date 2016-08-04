using TranslateWithDictCC.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

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

        RichTextBlock word1;
        RichTextBlock word2;

        public RichTextBlock Word1
        {
            get
            {
                if (word1 == null)
                    Initialize();

                return word1;
            }
        }

        public RichTextBlock Word2
        {
            get
            {
                if (word2 == null)
                    Initialize();

                return word2;
            }
        }

        static readonly SolidColorBrush annotationBrush = (SolidColorBrush)Application.Current.Resources["DictionaryEntryAnnotationThemeBrush"];
        static readonly SolidColorBrush queryBrush = (SolidColorBrush)Application.Current.Resources["DictionaryEntryQueryThemeBrush"];
        static readonly SolidColorBrush queryHighlightBrush = (SolidColorBrush)Application.Current.Resources["DictionaryEntryQueryHighlightThemeBrush"];

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
            word1 = GenerateRichTextBlock(reverseSearch ? DictionaryEntry.Word2 : DictionaryEntry.Word1, SearchContext.SearchQuery, true);
            word2 = GenerateRichTextBlock(reverseSearch ? DictionaryEntry.Word1 : DictionaryEntry.Word2, SearchContext.SearchQuery, false);
        }

        private RichTextBlock GenerateRichTextBlock(string word, string searchQuery, bool highlightQuery)
        {
            TextSpan[] annotationSpans = GetAnnotationSpans(word);

            RichTextBlock richTextBlock = new RichTextBlock();

            Paragraph paragraph = new Paragraph();

            TextSpan lastSpan = new TextSpan();

            int betweenOffset;
            int betweenLength;

            if (highlightQuery)
                for (int i = 0; i < DictionaryEntry.MatchSpans.Length; i++)
                {
                    TextSpan span = DictionaryEntry.MatchSpans[i];
                
                    betweenOffset = lastSpan.Offset + lastSpan.Length;
                    betweenLength = span.Offset - betweenOffset;
                
                    if (betweenLength != 0)
                        GenerateRun(paragraph, word, betweenOffset, betweenLength, annotationSpans);
                
#if true        
                    Rectangle rectangle = new Rectangle() { RadiusX = 4, RadiusY = 4, Fill = queryHighlightBrush };
                    TextBlock textBlock = new TextBlock() { Text = word.Substring(span.Offset, span.Length) };
                    textBlock.Margin = new Thickness(4, 0, 4, 0);

                    if (annotationSpans.Any(annotationSpan => annotationSpan.Contains(span)))
                        textBlock.Foreground = annotationBrush;

                    Grid grid = new Grid();
                    grid.Children.Add(rectangle);
                    grid.Children.Add(textBlock);
                    grid.Margin = new Thickness(0, -4, 0, 0);
                    grid.RenderTransform = new TranslateTransform() { Y = 4 };
                    paragraph.Inlines.Add(new InlineUIContainer() { Child = grid });
#else           
                    paragraph.Inlines.Add(new Run() { Text = word.Substring(span.Offset, span.Length), Foreground = queryBrush });
#endif          
                
                    lastSpan = span;
                }

            betweenOffset = lastSpan.Offset + lastSpan.Length;
            betweenLength = word.Length - betweenOffset;

            if (betweenLength != 0)
                GenerateRun(paragraph, word, betweenOffset, betweenLength, annotationSpans);

            richTextBlock.Blocks.Add(paragraph);

            return richTextBlock;
        }

        private void GenerateRun(Paragraph paragraph, string word, int offset, int length, TextSpan[] annotationSpans)
        {
            TextSpan[] affectedAnnotationSpans =
                annotationSpans
                .Where(span => span.Intersects(new TextSpan(offset, length)))
                .Select(span => new TextSpan(Math.Max(span.Offset, offset), Math.Min(span.Offset + span.Length, offset + length) - Math.Max(span.Offset, offset)))
                .ToArray();

            TextSpan lastSpan = new TextSpan(offset, 0);

            int betweenOffset;
            int betweenLength;

            for (int i = 0; i < affectedAnnotationSpans.Length; i++)
            {
                TextSpan span = affectedAnnotationSpans[i];

                betweenOffset = lastSpan.Offset + lastSpan.Length;
                betweenLength = span.Offset - betweenOffset;

                if (betweenLength != 0)
                    AddRun(paragraph, word, betweenOffset, betweenLength, false);

                AddRun(paragraph, word, span.Offset, span.Length, true);

                lastSpan = span;
            }

            betweenOffset = lastSpan.Offset + lastSpan.Length;
            betweenLength = offset + length - betweenOffset;

            if (betweenLength != 0)
                AddRun(paragraph, word, betweenOffset, betweenLength, false);
        }

        private void AddRun(Paragraph paragraph, string word, int offset, int length, bool annotation)
        {
            Run run = new Run() { Text = word.Substring(offset, length) };

            if (annotation)
                run.Foreground = annotationBrush;

            paragraph.Inlines.Add(run);
        }  

        private TextSpan[] GetAnnotationSpans(string word)
        {
            return Regex.Matches(word, @"(\{.*?\})|(\[.*?\])|(\<.*?\>)")
                .Cast<Match>()
                .Select(match => new TextSpan(match.Index, match.Length))
                .ToArray();
        }
    }

    enum AudioRecordingState
    {
        Available,
        Playing,
        Unavailable
    }
}

using TranslateWithDictCC.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Media.Core;
using Windows.Networking.Connectivity;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Documents;

namespace TranslateWithDictCC.Views
{
    public sealed partial class SearchResultsPage : Page
    {
        DictionaryEntryViewModel currentlyPlayingAudioRecording;
        bool currentlyPlayingAudioRecordingWord2;

        SemaphoreSlim audioPlayerSemaphore = new SemaphoreSlim(1);

        public SearchResultsPage()
        {
            InitializeComponent();

            //mediaElement.SetPlaybackSource(MediaSource.CreateFromStream(new MemoryStream().AsRandomAccessStream(), "audio/mpeg"));
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            //mediaElement.Source = null; // for some reason needed to prevent MediaElement from playing when navigating back to a page

            ((MainPage)((Frame)Window.Current.Content).Content).FocusSearchBox();

            if (e.Parameter != null)
            {
                DataContext = (SearchResultsViewModel)e.Parameter;

                ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

                int resultCount = ((SearchResultsViewModel)e.Parameter).DictionaryEntries.Count;

                if (resultCount == 1)
                    statusTextBlock.Text = resourceLoader.GetString("SearchResultsPage_SingleResult");
                else
                    statusTextBlock.Text = string.Format(resourceLoader.GetString("SearchResultsPage_ResultCount"), ((SearchResultsViewModel)e.Parameter).DictionaryEntries.Count);

                resultCountAnimation.Stop();
                resultCountAnimation.Seek(TimeSpan.Zero);
                await Task.Delay(250);
                resultCountAnimation.Begin();
            }
        }

        private async void audioRecordingButton1_Click(object sender, RoutedEventArgs e)
        {
            DictionaryEntryViewModel dictionaryEntryViewModel = (DictionaryEntryViewModel)((FrameworkElement)sender).DataContext;

            switch (dictionaryEntryViewModel.AudioRecordingState1)
            {
                case AudioRecordingState.Available:
                    await PlayAudioRecording(dictionaryEntryViewModel, false);
                    break;
                case AudioRecordingState.Playing:
                    //if (currentlyPlayingAudioRecording == dictionaryEntryViewModel)
                    //    mediaElement.Stop();
                    dictionaryEntryViewModel.AudioRecordingState1 = AudioRecordingState.Available;
                    break;
                case AudioRecordingState.Unavailable:
                    await ShowAudioRecordingNotAvailableMessage();
                    break;
            }      
        }

        private async void audioRecordingButton2_Click(object sender, RoutedEventArgs e)
        {
            DictionaryEntryViewModel dictionaryEntryViewModel = (DictionaryEntryViewModel)((FrameworkElement)sender).DataContext;

            switch (dictionaryEntryViewModel.AudioRecordingState2)
            {
                case AudioRecordingState.Available:
                    await PlayAudioRecording(dictionaryEntryViewModel, true);
                    break;
                case AudioRecordingState.Playing:
                    //if (currentlyPlayingAudioRecording == dictionaryEntryViewModel)
                    //    mediaElement.Stop();
                    dictionaryEntryViewModel.AudioRecordingState2 = AudioRecordingState.Available;
                    break;
                case AudioRecordingState.Unavailable:
                    await ShowAudioRecordingNotAvailableMessage();
                    break;
            }
        }

        private async Task PlayAudioRecording(DictionaryEntryViewModel dictionaryEntryViewModel, bool word2)
        {
            if (!IsInternetAccessAvailable())
            {
                ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

                MessageDialog messageDialog = new MessageDialog(
                    resourceLoader.GetString("No_Internet_Access_Body"),
                    resourceLoader.GetString("No_Internet_Access_Title"));

                await messageDialog.ShowAsync();
                return;
            }

            try
            {
                await audioPlayerSemaphore.WaitAsync();

                if (currentlyPlayingAudioRecording != null)
                {
                    //mediaElement.Stop();
                    if (currentlyPlayingAudioRecordingWord2)
                        currentlyPlayingAudioRecording.AudioRecordingState2 = AudioRecordingState.Available;
                    else
                        currentlyPlayingAudioRecording.AudioRecordingState1 = AudioRecordingState.Available;
                    currentlyPlayingAudioRecording = null;
                }

                Uri audioUri = await AudioRecordingFetcher.GetAudioRecordingUri(dictionaryEntryViewModel, word2);

                if (audioUri != null)
                {
                    currentlyPlayingAudioRecording = dictionaryEntryViewModel;
                    currentlyPlayingAudioRecordingWord2 = word2;

                    MediaSource mediaSource = MediaSource.CreateFromUri(audioUri);

                    //mediaElement.SetPlaybackSource(mediaSource);
                }
                else
                {
                    if (word2)
                        dictionaryEntryViewModel.AudioRecordingState2 = AudioRecordingState.Unavailable;
                    else
                        dictionaryEntryViewModel.AudioRecordingState1 = AudioRecordingState.Unavailable;
                }
            }
            catch
            {
                ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

                MessageDialog messageDialog = new MessageDialog(
                    resourceLoader.GetString("Error_Retrieving_Audio_Recording_Body"), 
                    resourceLoader.GetString("Error_Retrieving_Audio_Recording_Title"));

                await messageDialog.ShowAsync();
            }
            finally
            {
                audioPlayerSemaphore.Release();
            }
        }

        private async Task ShowAudioRecordingNotAvailableMessage()
        {
            ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

            MessageDialog messageDialog = new MessageDialog(
                resourceLoader.GetString("Audio_Recording_Not_Available_Body"),
                resourceLoader.GetString("Audio_Recording_Not_Available_Title"));

            await messageDialog.ShowAsync();
        }

        readonly SolidColorBrush altBackgroundThemeBrush = (SolidColorBrush)Application.Current.Resources["DictionaryEntryAltBackgroundThemeBrush"];

        private void ListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            void SetRichTextBlockContent(RichTextBlock richTextBlock, Block word)
            {
                if (richTextBlock.Blocks.Count == 0)
                    richTextBlock.Blocks.Add(word);
                else
                {
                    if (richTextBlock.Blocks[0] == word)
                    {
                        // clearing Blocks helps working around a bug with RichTextBlock when reusing Block elements
                        richTextBlock.Blocks.Clear();
                        return;
                    }

                    richTextBlock.Blocks[0] = word;
                }
            }

            if (args.ItemIndex % 2 == 0)
                args.ItemContainer.ClearValue(Control.BackgroundProperty);
            else
                args.ItemContainer.Background = altBackgroundThemeBrush;

            DictionaryEntryViewModel viewModel = (DictionaryEntryViewModel)args.Item;

            Grid templateRoot = (Grid)args.ItemContainer.ContentTemplateRoot;
            Grid grid = (Grid)templateRoot.Children[1];
            RichTextBlock word1RichTextBlock = (RichTextBlock)grid.Children[0];
            RichTextBlock word2RichTextBlock = (RichTextBlock)templateRoot.Children[2];

            SetRichTextBlockContent(word1RichTextBlock, viewModel.Word1);
            SetRichTextBlockContent(word2RichTextBlock, viewModel.Word2);
        }

        private bool IsInternetAccessAvailable()
        {
            try
            {
                ConnectionProfile profile = NetworkInformation.GetInternetConnectionProfile();

                return profile != null && profile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
            }
            catch
            {
                return false;
            }
        }

        private void hideResultCountButton_Click(object sender, RoutedEventArgs e)
        {
            resultCountAnimation.Stop();
            resultCountAnimation.Seek(TimeSpan.Zero);
        }

        /*private void mediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (currentlyPlayingAudioRecording != null)
                if (currentlyPlayingAudioRecordingWord2)
                    currentlyPlayingAudioRecording.AudioRecordingState2 = AudioRecordingState.Playing;
                else
                    currentlyPlayingAudioRecording.AudioRecordingState1 = AudioRecordingState.Playing;
        }

        private void mediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (currentlyPlayingAudioRecording != null)
            {
                if (currentlyPlayingAudioRecordingWord2)
                    currentlyPlayingAudioRecording.AudioRecordingState2 = AudioRecordingState.Available;
                else
                    currentlyPlayingAudioRecording.AudioRecordingState1 = AudioRecordingState.Available;
                currentlyPlayingAudioRecording = null;
            }
        }

        private void mediaElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (currentlyPlayingAudioRecording != null)
            {
                if (currentlyPlayingAudioRecordingWord2)
                    currentlyPlayingAudioRecording.AudioRecordingState2 = AudioRecordingState.Unavailable;
                else
                    currentlyPlayingAudioRecording.AudioRecordingState1 = AudioRecordingState.Unavailable;
                currentlyPlayingAudioRecording = null;
            }
        }*/
    }
}

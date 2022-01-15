using TranslateWithDictCC.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Windows.ApplicationModel.Resources;
using Windows.Media.Core;
using Windows.Networking.Connectivity;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Documents;
using Windows.Media.Playback;
using System.IO;
using Windows.System;
using System.ComponentModel;

namespace TranslateWithDictCC.Views
{
    public sealed partial class SearchResultsPage : Page
    {
        SearchResultsViewModel viewModel;

        SearchResultsViewModel ViewModel
        {
            get => viewModel;
            set { viewModel = value; Bindings.Update(); }
        }

        Settings Settings => Settings.Instance;

        DictionaryEntryViewModel currentlyPlayingAudioRecording;
        bool currentlyPlayingAudioRecordingWord2;

        MediaPlayer mediaPlayer = new MediaPlayer();
        SemaphoreSlim audioPlayerSemaphore = new SemaphoreSlim(1);

        public SearchResultsPage()
        {
            InitializeComponent();

            mediaPlayer.AudioCategory = MediaPlayerAudioCategory.Speech;
            mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
            mediaPlayer.AutoPlay = true;

            mediaPlayer.Source = MediaSource.CreateFromStream(new MemoryStream().AsRandomAccessStream(), "audio/mpeg");

            ViewModel = new SearchResultsViewModel();

            ViewModel.PropertyChanged += MainViewModel_PropertyChanged;

            KeyboardShortcutListener shortcutListener = new KeyboardShortcutListener();

            shortcutListener.RegisterShortcutHandler(VirtualKeyModifiers.Control, VirtualKey.E, OnControlEShortcut);
            shortcutListener.RegisterShortcutHandler(VirtualKeyModifiers.Control, VirtualKey.S, OnControlSShortcut);
        }

        private void OnControlEShortcut(object sender, EventArgs e)
        {
            searchBox.Text = string.Empty;
            FocusSearchBox();
        }

        public void FocusSearchBox()
        {
            searchBox.Focus(FocusState.Programmatic);
        }

        private void OnControlSShortcut(object sender, EventArgs e)
        {
            SwitchDirection_Click(sender, new RoutedEventArgs());
        }

        private void MainViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.SelectedDirection))
                SetDirectionComboBoxSelectedItem(ViewModel.SelectedDirection);
        }

        private void SetDirectionComboBoxSelectedItem(object selectedItem)
        {
            directionComboBox.SelectionChanged -= directionComboBox_SelectionChanged;

            directionComboBox.SelectedItem = selectedItem;

            directionComboBox.SelectionChanged += directionComboBox_SelectionChanged;
        }

        private void searchBox_Loaded(object sender, RoutedEventArgs e)
        {
            FocusSearchBox();
        }

        private async void CaseSensitiveSearch_Click(object sender, RoutedEventArgs e)
        {
            await PerformQuery();
        }

        private async void SwitchDirection_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SwitchDirectionOfTranslationCommand.Execute(null);

            await PerformQuery(dontSearchInBothDirections: true);
        }

        private async void searchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
                return;

            await ViewModel.UpdateSearchSuggestions(searchBox.Text);
        }

        private void RichTextBlock_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            RichTextBlock richTextBlock = (RichTextBlock)sender;
            SearchSuggestionViewModel searchSuggestionViewModel = (SearchSuggestionViewModel)args.NewValue;

            richTextBlock.Blocks.Clear();

            if (searchSuggestionViewModel == null)
                return;

            richTextBlock.Blocks.Add(searchSuggestionViewModel.Word);
        }

        private async void directionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectedDirection = directionComboBox.SelectedItem as DirectionViewModel;

            if (e.AddedItems.Count != 0 && e.RemovedItems.Count != 0)
                await PerformQuery(dontSearchInBothDirections: true);
        }

        private void directionComboBox_DropDownOpened(object sender, object e)
        {
            if (directionComboBox.Tag is double width)
                directionComboBox.Width = width;
        }

        private void directionComboBox_DropDownClosed(object sender, object e)
        {
            directionComboBox.ClearValue(FrameworkElement.WidthProperty);
        }

        private void directionComboBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!directionComboBox.IsDropDownOpen)
                directionComboBox.Tag = directionComboBox.ActualWidth;
        }

        private async Task PerformQuery(bool dontSearchInBothDirections = false)
        {
            if (searchBox.Text.Trim() == string.Empty || ViewModel.SelectedDirection == null)
                return;

            try
            {
                await ViewModel.PerformQuery(searchBox.Text, dontSearchInBothDirections);
            }
            catch
            {
                ResourceLoader resourceLoader = new ResourceLoader();

                ContentDialog contentDialog = new ContentDialog()
                {
                    Title = resourceLoader.GetString("Error_Performing_Query_Title"),
                    Content = resourceLoader.GetString("Error_Performing_Query_Body"),
                    CloseButtonText = "OK",
                    XamlRoot = MainWindow.Instance.Content.XamlRoot
                };

                await contentDialog.ShowAsync();
            }
        }

        private async void searchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            searchBox.Text = args.QueryText;
            await PerformQuery();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            mediaPlayer.Source = null; // for some reason needed to prevent MediaElement from playing when navigating back to a page

            FocusSearchBox();

            if (e.Parameter != null)
            {
                ViewModel = (SearchResultsViewModel)e.Parameter;

                if (ViewModel.SearchContext != null)
                    SetDirectionComboBoxSelectedItem(ViewModel.SearchContext.SelectedDirection);
                else
                    SetDirectionComboBoxSelectedItem(ViewModel.SelectedDirection);

                ResourceLoader resourceLoader = new ResourceLoader();

                if (ViewModel.DictionaryEntries != null)
                {
                    int resultCount = ViewModel.DictionaryEntries.Count;

                    if (resultCount == 1)
                        statusTextBlock.Text = resourceLoader.GetString("SearchResultsPage_SingleResult");
                    else
                        statusTextBlock.Text = string.Format(resourceLoader.GetString("SearchResultsPage_ResultCount"), ViewModel.DictionaryEntries.Count);

                    resultCountAnimation.Stop();
                    resultCountAnimation.Seek(TimeSpan.Zero);
                    await Task.Delay(250);
                    resultCountAnimation.Begin();
                }
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
                    if (currentlyPlayingAudioRecording == dictionaryEntryViewModel)
                        mediaPlayer.Pause();
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
                    if (currentlyPlayingAudioRecording == dictionaryEntryViewModel)
                        mediaPlayer.Pause();
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
                ResourceLoader resourceLoader = new ResourceLoader();

                ContentDialog contentDialog = new ContentDialog()
                {
                    Title = resourceLoader.GetString("No_Internet_Access_Title"),
                    Content = resourceLoader.GetString("No_Internet_Access_Body"),
                    CloseButtonText = "OK",
                    XamlRoot = MainWindow.Instance.Content.XamlRoot
                };

                await contentDialog.ShowAsync();
                return;
            }

            try
            {
                await audioPlayerSemaphore.WaitAsync();

                if (currentlyPlayingAudioRecording != null)
                {
                    mediaPlayer.Pause();
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

                    mediaPlayer.SetUriSource(audioUri);
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
                ResourceLoader resourceLoader = new ResourceLoader();

                ContentDialog contentDialog = new ContentDialog()
                {
                    Title = resourceLoader.GetString("Error_Retrieving_Audio_Recording_Title"),
                    Content = resourceLoader.GetString("Error_Retrieving_Audio_Recording_Body"),
                    CloseButtonText = "OK",
                    XamlRoot = MainWindow.Instance.Content.XamlRoot
                };

                await contentDialog.ShowAsync();
            }
            finally
            {
                audioPlayerSemaphore.Release();
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

            DictionaryEntryViewModel viewModel = (DictionaryEntryViewModel)args.Item;

            Grid templateRoot = (Grid)args.ItemContainer.ContentTemplateRoot;
            Grid grid = (Grid)templateRoot.Children[2];
            RichTextBlock word1RichTextBlock = (RichTextBlock)grid.Children[0];
            RichTextBlock word2RichTextBlock = (RichTextBlock)templateRoot.Children[3];

            SetRichTextBlockContent(word1RichTextBlock, viewModel.Word1);
            SetRichTextBlockContent(word2RichTextBlock, viewModel.Word2);

            Border border = (Border)templateRoot.Children[0];
            if (args.ItemIndex % 2 == 0)
                border.ClearValue(Border.BackgroundProperty);
            else
                border.Background = altBackgroundThemeBrush;
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

        private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (currentlyPlayingAudioRecording != null)
                    if (currentlyPlayingAudioRecordingWord2)
                        currentlyPlayingAudioRecording.AudioRecordingState2 = AudioRecordingState.Playing;
                    else
                        currentlyPlayingAudioRecording.AudioRecordingState1 = AudioRecordingState.Playing;
            });
        }

        private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (currentlyPlayingAudioRecording != null)
                {
                    if (currentlyPlayingAudioRecordingWord2)
                        currentlyPlayingAudioRecording.AudioRecordingState2 = AudioRecordingState.Available;
                    else
                        currentlyPlayingAudioRecording.AudioRecordingState1 = AudioRecordingState.Available;
                    currentlyPlayingAudioRecording = null;
                }
            });
        }

        private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (currentlyPlayingAudioRecording != null)
                {
                    if (currentlyPlayingAudioRecordingWord2)
                        currentlyPlayingAudioRecording.AudioRecordingState2 = AudioRecordingState.Unavailable;
                    else
                        currentlyPlayingAudioRecording.AudioRecordingState1 = AudioRecordingState.Unavailable;
                    currentlyPlayingAudioRecording = null;
                }
            });
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            const double widthThreshold = 650;

            if (e.NewSize.Width < widthThreshold != e.PreviousSize.Width < widthThreshold)
            {
                if (e.NewSize.Width < widthThreshold)
                    directionComboBox.ItemTemplate = (DataTemplate)Resources["DirectionComboBoxItemTemplateCompact"];
                else
                    directionComboBox.ItemTemplate = (DataTemplate)Resources["DirectionComboBoxItemTemplate"];

                // force ComboBox to apply the new item template
                directionComboBox.SelectionChanged -= directionComboBox_SelectionChanged;
                object selectedItem = directionComboBox.SelectedItem;
                directionComboBox.SelectedItem = null;
                directionComboBox.SelectedItem = selectedItem;
                directionComboBox.SelectionChanged += directionComboBox_SelectionChanged;
            }
        }
    }
}

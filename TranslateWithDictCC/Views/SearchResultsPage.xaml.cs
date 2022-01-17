using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TranslateWithDictCC.ViewModels;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Networking.Connectivity;
using Windows.System;

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

        public SearchResultsPage()
        {
            InitializeComponent();

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

        private void hideResultCountButton_Click(object sender, RoutedEventArgs e)
        {
            resultCountAnimation.Stop();
            resultCountAnimation.Seek(TimeSpan.Zero);
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

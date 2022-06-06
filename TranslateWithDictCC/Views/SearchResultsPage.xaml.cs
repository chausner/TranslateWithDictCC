using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using TranslateWithDictCC.ViewModels;
using Windows.System;

namespace TranslateWithDictCC.Views
{
    public sealed partial class SearchResultsPage : Page
    {
        SearchResultsViewModel ViewModel => SearchResultsViewModel.Instance;

        Settings Settings => Settings.Instance;

        bool isPageBeingShown;
        string lastQuery;

        public SearchResultsPage()
        {
            InitializeComponent();

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            KeyboardShortcutListener shortcutListener = new KeyboardShortcutListener(MainWindow.Instance.ApplicationFrame);

            shortcutListener.RegisterShortcutHandler(VirtualKeyModifiers.Control, VirtualKey.E, OnControlEShortcut);
            shortcutListener.RegisterShortcutHandler(VirtualKeyModifiers.Control, VirtualKey.S, OnControlSShortcut);
        }

        private void OnControlEShortcut(object sender, KeyRoutedEventArgs e)
        {
            searchBox.Text = string.Empty;
            FocusSearchBox();
            e.Handled = true;
        }

        public void FocusSearchBox()
        {
            searchBox.Focus(FocusState.Programmatic);
        }

        private void OnControlSShortcut(object sender, KeyRoutedEventArgs e)
        {
            SwitchDirection_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
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

        private void CaseSensitiveSearch_Click(object sender, RoutedEventArgs e)
        {
            PerformQuery();
        }

        private void SwitchDirection_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SwitchDirectionOfTranslationCommand.Execute(null);

            PerformQuery(dontSearchInBothDirections: true);
        }

        private async void searchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
                return;

            // the TextChanged event can fire after the corresponding QuerySubmitted event,
            // in this case, we don't want to show any suggestions anymore
            if (searchBox.Text == lastQuery)
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

        private void directionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectedDirection = directionComboBox.SelectedItem as DirectionViewModel;

            if (e.AddedItems.Count != 0 && e.RemovedItems.Count != 0)
                PerformQuery(dontSearchInBothDirections: true);
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

        private void PerformQuery(bool dontSearchInBothDirections = false)
        {
            if (string.IsNullOrWhiteSpace(searchBox.Text) || ViewModel.SelectedDirection == null)
                return;

            SearchContext searchContext = new SearchContext(searchBox.Text, ViewModel.SelectedDirection, dontSearchInBothDirections);

            // explicit type parameters required here
            MainViewModel.Instance.NavigateToPageCommand.Execute(Tuple.Create<string, object>("SearchResultsPage", searchContext));
        }

        private void searchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            searchBox.Text = args.QueryText;
            lastQuery = args.QueryText;
            PerformQuery();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            isPageBeingShown = true;

            FocusSearchBox();

            SearchContext searchContext = (SearchContext)e.Parameter;

            if (searchContext != null)
            {
                searchBox.Text = searchContext.SearchQuery;
                ViewModel.SelectedDirection = searchContext.SelectedDirection;
            }
            else
                SetDirectionComboBoxSelectedItem(ViewModel.SelectedDirection);

            if (searchContext != null && !string.IsNullOrEmpty(searchContext.SearchQuery))
            {
                ResourceLoader resourceLoader = new ResourceLoader();

                try
                {
                    await ViewModel.PerformQuery(searchContext.SearchQuery, searchContext.DontSearchInBothDirections);
                }
                catch
                {
                    ContentDialog contentDialog = new ContentDialog()
                    {
                        Title = resourceLoader.GetString("Error_Performing_Query_Title"),
                        Content = resourceLoader.GetString("Error_Performing_Query_Body"),
                        CloseButtonText = "OK",
                        XamlRoot = MainWindow.Instance.Content.XamlRoot
                    };

                    await contentDialog.ShowAsync();
                }

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

            if (!MainViewModel.Instance.NoDictionaryInstalledTeachingTipShown && ViewModel.AvailableDirections.Length == 0)
            {
                // workaround https://github.com/microsoft/microsoft-ui-xaml/issues/6628
                await Task.Delay(1000);

                // make sure that the user hasn't navigated to another page in the meantime
                if (isPageBeingShown)
                {
                    MainViewModel.Instance.ShowNoDictionaryInstalledTeachingTip = true;
                    MainViewModel.Instance.NoDictionaryInstalledTeachingTipShown = true;
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (e.SourcePageType != typeof(SearchResultsPage))
            {
                MainViewModel.Instance.ShowNoDictionaryInstalledTeachingTip = false;
                isPageBeingShown = false;
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

            StackPanel stackPanel = (StackPanel)grid.Children[1];
            stackPanel.Children.Clear();
            foreach (UIElement element in viewModel.Subjects)
                stackPanel.Children.Add(element);

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

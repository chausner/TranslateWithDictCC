using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using TranslateWithDictCC.ViewModels;
using Windows.System;

namespace TranslateWithDictCC.Views;

public sealed partial class SearchResultsPage : Page
{
    private sealed class DictionaryEntryTemplateParts
    {
        public required Border BackgroundBorder { get; init; }
        public required RichTextBlock Word1RichTextBlock { get; init; }
        public required RichTextBlock Word2RichTextBlock { get; init; }
        public required StackPanel AttributesPanel { get; init; }
    }

    SearchResultsViewModel ViewModel => SearchResultsViewModel.Instance;

    Settings Settings => Settings.Instance;

    readonly SolidColorBrush altBackgroundThemeBrush = (SolidColorBrush)Application.Current.Resources["DictionaryEntryAltBackgroundThemeBrush"];
    readonly Brush wordClassesBorderBackground = (Brush)Application.Current.Resources["DictionaryEntryWordClassesThemeBrush"];
    readonly double wordClassesFontSize = (double)Application.Current.Resources["wordFontSize"];

    string? lastQuery;

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

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.SelectedDirection))
            SetDirectionComboBoxSelectedItem(ViewModel.SelectedDirection);
    }

    private void SetDirectionComboBoxSelectedItem(DirectionViewModel? selectedItem)
    {
        directionComboBox.SelectionChanged -= directionComboBox_SelectionChanged;

        directionComboBox.SelectedItem = selectedItem;

        directionComboBox.SelectionChanged += directionComboBox_SelectionChanged;
    }

    private void searchBox_Loaded(object sender, RoutedEventArgs e)
    {
        FocusSearchBox();
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
        SearchSuggestionViewModel? searchSuggestionViewModel = args.NewValue as SearchSuggestionViewModel;

        WordHighlighting.ClearRichTextBlockContent(richTextBlock);

        if (searchSuggestionViewModel == null)
            return;

        WordHighlighting.SetRichTextBlockContent(richTextBlock, searchSuggestionViewModel.Word);
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
        FocusSearchBox();

        SearchContext? searchContext = e.Parameter as SearchContext;

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
            MainViewModel.Instance.ShowNoDictionaryInstalledTeachingTip = true;
            MainViewModel.Instance.NoDictionaryInstalledTeachingTipShown = true;
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        if (e.SourcePageType != typeof(SearchResultsPage))
        {
            MainViewModel.Instance.ShowNoDictionaryInstalledTeachingTip = false;
        }
    }

    private void ListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.ItemContainer.ContentTemplateRoot is not Grid templateRoot)
            return;

        DictionaryEntryTemplateParts templateParts = GetTemplateParts(templateRoot);

        if (args.InRecycleQueue)
        {
            WordHighlighting.ClearRichTextBlockContent(templateParts.Word1RichTextBlock);
            WordHighlighting.ClearRichTextBlockContent(templateParts.Word2RichTextBlock);
            ClearAttributes(templateParts.AttributesPanel);
            return;
        }

        args.Handled = true;

        DictionaryEntryViewModel viewModel = (DictionaryEntryViewModel)args.Item;

        switch (args.Phase)
        {
            case 0:
                if (args.ItemIndex % 2 == 0)
                    templateParts.BackgroundBorder.ClearValue(Border.BackgroundProperty);
                else
                    templateParts.BackgroundBorder.Background = altBackgroundThemeBrush;

                WordHighlighting.ClearRichTextBlockContent(templateParts.Word1RichTextBlock);
                WordHighlighting.ClearRichTextBlockContent(templateParts.Word2RichTextBlock);
                ClearAttributes(templateParts.AttributesPanel);

                args.RegisterUpdateCallback(ListView_ContainerContentChanging);
                break;

            case 1:
                WordHighlighting.SetRichTextBlockContent(templateParts.Word1RichTextBlock, viewModel.Word1);
                args.RegisterUpdateCallback(ListView_ContainerContentChanging);
                break;

            case 2:
                WordHighlighting.SetRichTextBlockContent(templateParts.Word2RichTextBlock, viewModel.Word2);

                if (viewModel.Attributes.Count != 0)
                    args.RegisterUpdateCallback(ListView_ContainerContentChanging);
                break;

            case 3:
                SetAttributes(templateParts.AttributesPanel, viewModel.Attributes);
                break;
        }
    }

    private static DictionaryEntryTemplateParts GetTemplateParts(Grid templateRoot)
    {
        if (templateRoot.Tag is DictionaryEntryTemplateParts templateParts)
            return templateParts;

        templateParts = new DictionaryEntryTemplateParts()
        {
            BackgroundBorder = (Border)templateRoot.FindName("RowBackgroundBorder"),
            Word1RichTextBlock = (RichTextBlock)templateRoot.FindName("Word1RichTextBlock"),
            Word2RichTextBlock = (RichTextBlock)templateRoot.FindName("Word2RichTextBlock"),
            AttributesPanel = (StackPanel)templateRoot.FindName("AttributesPanel")
        };

        templateRoot.Tag = templateParts;
        return templateParts;
    }

    private static void ClearAttributes(StackPanel attributesPanel)
    {
        attributesPanel.Tag = null;
        attributesPanel.Children.Clear();
    }

    private void SetAttributes(StackPanel attributesPanel, IReadOnlyList<DictionaryEntryAttribute> attributes)
    {
        if (ReferenceEquals(attributesPanel.Tag, attributes))
            return;

        ClearAttributes(attributesPanel);

        for (int i = 0; i < attributes.Count; i++)
            attributesPanel.Children.Add(CreateAttributeElement(attributes[i], i != 0));

        attributesPanel.Tag = attributes;
    }

    private UIElement CreateAttributeElement(DictionaryEntryAttribute attribute, bool hasLeftMargin)
    {
        Border border = new Border()
        {
            Background = wordClassesBorderBackground,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(5, 2, 5, 2)
        };

        if (hasLeftMargin)
            border.Margin = new Thickness(5, 0, 0, 0);

        border.Child = new TextBlock()
        {
            FontSize = wordClassesFontSize,
            Text = attribute.Text
        };

        if (attribute.ToolTipText != null)
            ToolTipService.SetToolTip(border, attribute.ToolTipText);

        return border;
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
            object? selectedItem = directionComboBox.SelectedItem;
            directionComboBox.SelectedItem = null;
            directionComboBox.SelectedItem = selectedItem;
            directionComboBox.SelectionChanged += directionComboBox_SelectionChanged;
        }
    }

    private void MoreButton1_Click(object sender, RoutedEventArgs e)
    {
        DictionaryEntryViewModel viewModel = (DictionaryEntryViewModel)((FrameworkElement)sender).DataContext;

        if (viewModel.AudioRecordingState1 != AudioRecordingState.Playing)
        {
            MenuFlyout flyout = (MenuFlyout)Resources["MoreButton1Flyout"];
            flyout.ShowAt((FrameworkElement)sender);
        }
        else
            if (viewModel.PlayStopAudioRecording1Command.CanExecute(null))
            viewModel.PlayStopAudioRecording1Command.Execute(null);
    }

    private void MoreButton2_Click(object sender, RoutedEventArgs e)
    {
        DictionaryEntryViewModel viewModel = (DictionaryEntryViewModel)((FrameworkElement)sender).DataContext;

        if (viewModel.AudioRecordingState2 != AudioRecordingState.Playing)
        {
            MenuFlyout flyout = (MenuFlyout)Resources["MoreButton2Flyout"];
            flyout.ShowAt((FrameworkElement)sender);
        }
        else
            if (viewModel.PlayStopAudioRecording2Command.CanExecute(null))
                viewModel.PlayStopAudioRecording2Command.Execute(null);
    }
}

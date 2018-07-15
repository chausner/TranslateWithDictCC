using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TranslateWithDictCC.ViewModels;
using Windows.ApplicationModel.Resources;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace TranslateWithDictCC.Views
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();

            DataContext = MainViewModel.Instance;

            CoreWindow.GetForCurrentThread().PointerPressed += MainPage_PointerPressed;

            SystemNavigationManager.GetForCurrentView().BackRequested += SystemNavigationManager_BackRequested;

            MainViewModel.Instance.NavigateToPageCommand = new RelayCommand<object>(NavigateToPage);
            MainViewModel.Instance.GoBackToPageCommand = new RelayCommand<string>(GoBackToPage);

            MainViewModel.Instance.PropertyChanged += MainViewModel_PropertyChanged;

            KeyboardShortcutListener shortcutListener = new KeyboardShortcutListener();

            shortcutListener.RegisterShortcutHandler(VirtualKeyModifiers.Control, VirtualKey.E, ControlEShortcut);
            shortcutListener.RegisterShortcutHandler(VirtualKeyModifiers.Control, VirtualKey.S, ControlSShortcut);
        }

        private void ControlEShortcut(object sender, EventArgs e)
        {
            searchBox.Text = string.Empty;
            FocusSearchBox();
        }

        private void ControlSShortcut(object sender, EventArgs e)
        {
            SwitchDirection_Click(sender, new RoutedEventArgs());
        }

        private void MainViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.Instance.SelectedDirection))
                SetDirectionComboBoxSelectedItem(MainViewModel.Instance.SelectedDirection);
        }

        private void SetDirectionComboBoxSelectedItem(object selectedItem)
        {
            directionComboBox.SelectionChanged -= directionComboBox_SelectionChanged;

            directionComboBox.SelectedItem = selectedItem;

            directionComboBox.SelectionChanged += directionComboBox_SelectionChanged;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (MainViewModel.Instance.AvailableDirections.Length == 0)
                contentFrame.Navigate(typeof(SettingsPage));
            else
            {
                string launchArguments = e.Parameter as string;

                if (!string.IsNullOrEmpty(launchArguments))
                {
                    if (launchArguments.StartsWith("dict:") && launchArguments.Length == 9)
                    {
                        string originLanguageCode = launchArguments.Substring(5, 2);
                        string destinationLanguageCode = launchArguments.Substring(7, 2);

                        DirectionViewModel directionViewModel =
                            MainViewModel.Instance.AvailableDirections.FirstOrDefault(
                                dvm => dvm.OriginLanguageCode == originLanguageCode && dvm.DestinationLanguageCode == destinationLanguageCode);

                        if (directionViewModel != null)
                        {
                            MainViewModel.Instance.SelectedDirection = directionViewModel;
                            searchBox.Text = string.Empty;
                        }
                    }
                }
            }
        }

        private void GoBackToPage(string pageType)
        {
            GoBackToPage(typeof(SearchResultsPage));
        }

        private void GoBackToPage(Type pageType)
        {
            while (contentFrame.SourcePageType != pageType)
            {
                if (contentFrame.CanGoBack)
                    contentFrame.GoBack();
                else
                    contentFrame.Navigate(pageType);
            }
        }

        private void NavigateToPage(object pageTypeAndParameter)
        {           
            string pageType;
            object parameter;

            if (pageTypeAndParameter is string)
            {
                pageType = (string)pageTypeAndParameter;
                parameter = null;
            }
            else
            {
                pageType = ((Tuple<string, object>)pageTypeAndParameter).Item1;
                parameter = ((Tuple<string, object>)pageTypeAndParameter).Item2;
            }

            if (pageType == "SearchResultsPage")
                contentFrame.Navigate(typeof(SearchResultsPage), parameter);
            else if (pageType == "SettingsPage")
                contentFrame.NavigateIfNeeded(typeof(SettingsPage), parameter);
            else if (pageType == "AboutPage")
                contentFrame.NavigateIfNeeded(typeof(AboutPage), parameter);
        }

        public void FocusSearchBox()
        {
            searchBox.Focus(FocusState.Programmatic);
        }

        private void MainPage_PointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            if (args.CurrentPoint.Properties.IsXButton1Pressed)
            {
                if (contentFrame.CanGoBack)
                {
                    contentFrame.GoBack();
                    args.Handled = true;
                }
            }
            //else if (args.CurrentPoint.Properties.IsXButton2Pressed)
            //{
            //    if (contentFrame.CanGoForward)
            //    {
            //        contentFrame.GoForward();
            //        args.Handled = true;
            //    }
            //}
        }

        private void SystemNavigationManager_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (contentFrame.CanGoBack)
            {
                contentFrame.GoBack();
                e.Handled = true;
            }
        }

        private async Task PerformQuery(bool dontSearchInBothDirections = false)
        {
            if (searchBox.Text.Trim() == string.Empty || MainViewModel.Instance.SelectedDirection == null)
                return;

            try
            {
                await MainViewModel.Instance.PerformQuery(searchBox.Text, dontSearchInBothDirections);
            }
            catch
            {
                ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

                MessageDialog messageDialog = new MessageDialog(
                    resourceLoader.GetString("Error_Performing_Query_Body"), 
                    resourceLoader.GetString("Error_Performing_Query_Title"));

                await messageDialog.ShowAsync();
            }
        }

        private async void searchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            searchBox.Text = args.QueryText;
            await PerformQuery();
        }      

        private void hamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            splitView.IsPaneOpen = !splitView.IsPaneOpen;
        }

        private void contentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                contentFrame.CanGoBack ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;

            if (e.SourcePageType == typeof(SearchResultsPage))
            {
                SearchResultsViewModel searchResultsViewModel = (SearchResultsViewModel)e.Parameter;

                if (searchResultsViewModel != null)
                {
                    searchBox.Text = searchResultsViewModel.SearchContext.SearchQuery;
                    SetDirectionComboBoxSelectedItem(searchResultsViewModel.SearchContext.SelectedDirection);
                }
            }

            searchHamburgerMenuItem.IsChecked = e.SourcePageType == typeof(SearchResultsPage);
            optionsHamburgerMenuItem.IsChecked = e.SourcePageType == typeof(SettingsPage);
            aboutHamburgerMenuItem.IsChecked = e.SourcePageType == typeof(AboutPage);
        }

        private async void directionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MainViewModel.Instance.SelectedDirection = directionComboBox.SelectedItem as DirectionViewModel;

            if (e.AddedItems.Count != 0 && e.RemovedItems.Count != 0)
                await PerformQuery(dontSearchInBothDirections: true);
        }

        private void searchBox_Loaded(object sender, RoutedEventArgs e)
        {
            FocusSearchBox();
        }

        private void moreButton_Click(object sender, RoutedEventArgs e)
        {
            moreMenuFlyout.ShowAt((Button)sender);
        }

        private async void CaseSensitiveSearch_Click(object sender, RoutedEventArgs e)
        {
            await PerformQuery();
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width < 600 != e.PreviousSize.Width < 600)
            {
                if (e.NewSize.Width < 600)
                    directionComboBox.ItemTemplate = (DataTemplate)Resources["DirectionComboBoxItemTemplateCompact"];
                else
                    directionComboBox.ItemTemplate = (DataTemplate)Resources["DirectionComboBoxItemTemplate"];

                // force ComboBox to apply the new item template
                DirectionViewModel selectedDirection = MainViewModel.Instance.SelectedDirection;
                MainViewModel.Instance.SelectedDirection = null;
                MainViewModel.Instance.SelectedDirection = selectedDirection;
            }
        }

        private void searchHamburgerMenuItem_CheckRequested(object sender, EventArgs e)
        {
            splitView.IsPaneOpen = false;

            GoBackToPage(typeof(SearchResultsPage));
        }

        private void optionsHamburgerMenuItem_CheckRequested(object sender, EventArgs e)
        {
            splitView.IsPaneOpen = false;

            NavigateToPage("SettingsPage");
        }

        private void aboutHamburgerMenuItem_CheckRequested(object sender, EventArgs e)
        {
            splitView.IsPaneOpen = false;

            NavigateToPage("AboutPage");
        }

        private async void SwitchDirection_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance.SwitchDirectionOfTranslationCommand.Execute(null);

            await PerformQuery(dontSearchInBothDirections: true);
        }

        private async void searchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
                return;

            await MainViewModel.Instance.UpdateSearchSuggestions(searchBox.Text);
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
    }
}

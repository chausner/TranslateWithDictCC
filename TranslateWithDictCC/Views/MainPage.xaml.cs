using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using TranslateWithDictCC.ViewModels;

namespace TranslateWithDictCC.Views
{
    public sealed partial class MainPage : Page
    {
        MainViewModel ViewModel => MainViewModel.Instance;

        public MainPage()
        {
            InitializeComponent();

            rootGrid.DataContext = MainViewModel.Instance;

            ViewModel.NavigateToPageCommand = new RelayCommand<object>(o => NavigateToPage(o, new SuppressNavigationTransitionInfo()));
            ViewModel.GoBackToPageCommand = new RelayCommand<string>(o => GoBackToPage(o, new SuppressNavigationTransitionInfo()));

            PointerPressed += MainPage_PointerPressed;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            SearchContext searchContext = null;

            string launchArguments = e.Parameter as string;

            if (!string.IsNullOrEmpty(launchArguments))
            {
                if (launchArguments.StartsWith("dict:") && launchArguments.Length == 9)
                {
                    string originLanguageCode = launchArguments.Substring(5, 2);
                    string destinationLanguageCode = launchArguments.Substring(7, 2);

                    DirectionViewModel directionViewModel =
                        SearchResultsViewModel.Instance.AvailableDirections.FirstOrDefault(
                            dvm => dvm.OriginLanguageCode == originLanguageCode && dvm.DestinationLanguageCode == destinationLanguageCode);

                    if (directionViewModel != null)
                        searchContext = new SearchContext(null, directionViewModel, false);
                }
            }

            contentFrame.Navigate(typeof(SearchResultsPage), searchContext);
        }

        private void GoBackToPage(string pageType, NavigationTransitionInfo transitionInfo)
        {
            GoBackToPage(typeof(SearchResultsPage), transitionInfo);
        }

        private void GoBackToPage(Type pageType, NavigationTransitionInfo transitionInfo)
        {
            while (contentFrame.SourcePageType != pageType)
            {
                if (contentFrame.CanGoBack)
                    contentFrame.GoBack(transitionInfo);
                else
                    contentFrame.Navigate(pageType, null, transitionInfo);
            }
        }

        private void NavigateToPage(object pageTypeAndParameter, NavigationTransitionInfo transitionInfo)
        {           
            string pageType;
            object parameter;

            if (pageTypeAndParameter is string s)
            {
                pageType = s;
                parameter = null;
            }
            else if (pageTypeAndParameter is Tuple<string, object> t)
            {
                pageType = t.Item1;
                parameter = t.Item2;
            }
            else
                throw new ArgumentException();

            if (pageType == "SearchResultsPage")
                contentFrame.Navigate(typeof(SearchResultsPage), parameter, transitionInfo);
            else if (pageType == "SettingsPage")
                contentFrame.NavigateIfNeeded(typeof(SettingsPage), parameter, transitionInfo);
            else if (pageType == "AboutPage")
                contentFrame.NavigateIfNeeded(typeof(AboutPage), parameter, transitionInfo);
        }

        private void MainPage_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint point = e.GetCurrentPoint(null);

            if (point.Properties.IsXButton1Pressed)
            {
                if (contentFrame.CanGoBack)
                {
                    contentFrame.GoBack();
                    e.Handled = true;
                }
            }
            else if (point.Properties.IsXButton2Pressed)
            {
                if (contentFrame.CanGoForward)
                {
                    contentFrame.GoForward();
                    e.Handled = true;
                }
            }
        }        

        private void contentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            searchHamburgerMenuItem.IsSelected = e.SourcePageType == typeof(SearchResultsPage);
            optionsHamburgerMenuItem.IsSelected = e.SourcePageType == typeof(SettingsPage);
            aboutHamburgerMenuItem.IsSelected = e.SourcePageType == typeof(AboutPage);
        }

        private void navigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            navigationView.IsPaneOpen = false;

            if (args.InvokedItemContainer == searchHamburgerMenuItem)
                NavigateToPage("SearchResultsPage", new SuppressNavigationTransitionInfo());
            else if (args.InvokedItemContainer == optionsHamburgerMenuItem)
                NavigateToPage("SettingsPage", new SuppressNavigationTransitionInfo());
            else if (args.InvokedItemContainer == aboutHamburgerMenuItem)
                NavigateToPage("AboutPage", new SuppressNavigationTransitionInfo());
        }

        private void navigationView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (contentFrame.CanGoBack)
                contentFrame.GoBack();
        }
    }
}

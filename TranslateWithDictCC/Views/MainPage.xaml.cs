using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System.Linq;
using TranslateWithDictCC.ViewModels;

namespace TranslateWithDictCC.Views;

public sealed partial class MainPage : Page
{
    MainViewModel ViewModel { get; }

    public MainPage()
    {
        InitializeComponent();

        ViewModel = ((App)App.Current).Host.Services.GetRequiredService<MainViewModel>();

        rootGrid.DataContext = ViewModel;

        PointerPressed += MainPage_PointerPressed;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        SearchContext? searchContext = null;

        string? launchArguments = e.Parameter as string;

        if (!string.IsNullOrEmpty(launchArguments))
        {
            if (launchArguments.StartsWith("dict:") && launchArguments.Length == 9)
            {
                string originLanguageCode = launchArguments.Substring(5, 2);
                string destinationLanguageCode = launchArguments.Substring(7, 2);

                SearchResultsViewModel searchResultsViewModel = ((App)App.Current).Host.Services.GetRequiredService<SearchResultsViewModel>();

                DirectionViewModel? directionViewModel =
                    searchResultsViewModel.AvailableDirections.FirstOrDefault(
                        dvm => dvm.OriginLanguageCode == originLanguageCode && dvm.DestinationLanguageCode == destinationLanguageCode);

                if (directionViewModel != null)
                    searchContext = new SearchContext(null, directionViewModel, false);
            }
        }

        NavigateToPage<SearchResultsPage>(searchContext);
    }

    public void NavigateToPage<TPage>(object? parameter = null)
    {
        contentFrame.Navigate(typeof(TPage), parameter, new SuppressNavigationTransitionInfo());
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
            NavigateToPage<SearchResultsPage>();
        else if (args.InvokedItemContainer == optionsHamburgerMenuItem)
            NavigateToPage<SettingsPage>();
        else if (args.InvokedItemContainer == aboutHamburgerMenuItem)
            NavigateToPage<AboutPage>();
    }

    private void navigationView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        if (contentFrame.CanGoBack)
            contentFrame.GoBack();
    }
}

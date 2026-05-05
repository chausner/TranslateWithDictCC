using TranslateWithDictCC.ViewModels;
using TranslateWithDictCC.Views;

namespace TranslateWithDictCC.Services;

class NavigationService
{
    public void NavigateToSearchResultsPage(SearchContext searchContext)
    {
        MainPage mainPage = (MainPage)MainWindow.Instance.ApplicationFrame.Content;
        mainPage.NavigateToPage<SearchResultsPage>(searchContext);
    }

    public void NavigateToSettingsPage()
    {
        MainPage mainPage = (MainPage)MainWindow.Instance.ApplicationFrame.Content;
        mainPage.NavigateToPage<SettingsPage>();
    }

    public void NavigateToAboutPage()
    {
        MainPage mainPage = (MainPage)MainWindow.Instance.ApplicationFrame.Content;
        mainPage.NavigateToPage<AboutPage>();
    }
}

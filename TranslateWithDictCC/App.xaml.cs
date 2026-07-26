using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using TranslateWithDictCC.ViewModels;

namespace TranslateWithDictCC;

sealed partial class App : Application
{
    public App()
    {
        InitializeComponent();

        if (Settings.Instance.AppTheme == ElementTheme.Light)
            RequestedTheme = ApplicationTheme.Light;
        else if (Settings.Instance.AppTheme == ElementTheme.Dark)
            RequestedTheme = ApplicationTheme.Dark;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs e)
    {
        await ApplicationDataMigration.Migrate();
        await DatabaseManager.Instance.InitializeDb();
        await SettingsViewModel.Instance.Load();
        await SearchResultsViewModel.Instance.Load();

        _ = Task.Run(() => SubjectInfo.Instance.LoadAsync());

        if (!Resources.ContainsKey("settings"))
            Resources.Add("settings", Settings.Instance);

        string[] args = Environment.GetCommandLineArgs();
        string? launchArguments = args.Length >= 2 ? args[1] : null;

        MainWindow window = new MainWindow(launchArguments);

        window.Activate();
        window.AppWindow.Closing += AppWindow_Closing;
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        SearchResultsViewModel.Instance.SaveSettings();
    }
}

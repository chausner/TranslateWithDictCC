using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Threading.Tasks;
using TranslateWithDictCC.Services;
using TranslateWithDictCC.ViewModels;
using Windows.Storage;
using Windows.Web.Http;

namespace TranslateWithDictCC;

sealed partial class App : Application
{
    MainWindow? mainWindow;

    public IHost Host { get; }

    public App()
    {
        InitializeComponent();

        HostApplicationBuilder builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();

        ConfigureServices(builder.Services);

        Host = builder.Build();
        Host.Start();

        Settings settings = Host.Services.GetRequiredService<Settings>();

        if (settings.AppTheme == ElementTheme.Light)
            RequestedTheme = ApplicationTheme.Light;
        else if (settings.AppTheme == ElementTheme.Dark)
            RequestedTheme = ApplicationTheme.Dark;
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new DatabaseManager(Path.Combine(ApplicationData.Current.LocalFolder.Path, "dictionaries.db")));
        services.AddSingleton<ApplicationDataMigration>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<SearchResultsViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<WordHighlighting>();
        services.AddSingleton<Settings>();
        services.AddSingleton<SubjectInfo>();
        services.AddSingleton<HttpClient>();
        services.AddSingleton<AudioRecordingFetcher>();
        services.AddSingleton<AudioPlayer>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<DialogService>();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs e)
    {
        ApplicationDataMigration applicationDataMigration = Host.Services.GetRequiredService<ApplicationDataMigration>();
        DatabaseManager databaseManager = Host.Services.GetRequiredService<DatabaseManager>();
        SettingsViewModel settingsViewModel = Host.Services.GetRequiredService<SettingsViewModel>();
        SearchResultsViewModel searchResultsViewModel = Host.Services.GetRequiredService<SearchResultsViewModel>();
        Settings settings = Host.Services.GetRequiredService<Settings>();
        SubjectInfo subjectInfo = Host.Services.GetRequiredService<SubjectInfo>();

        await applicationDataMigration.Migrate();
        await databaseManager.InitializeDb();
        await settingsViewModel.Load();
        await searchResultsViewModel.Load();

        _ = Task.Run(() => subjectInfo.LoadAsync());

        if (!Resources.ContainsKey("settings"))
            Resources.Add("settings", settings);

        string[] args = Environment.GetCommandLineArgs();
        string? launchArguments = args.Length >= 2 ? args[1] : null;

        mainWindow = new MainWindow(settings, launchArguments);

        mainWindow.Activate();
        mainWindow.AppWindow.Closing += AppWindow_Closing;
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        SearchResultsViewModel searchResultsViewModel = Host.Services.GetRequiredService<SearchResultsViewModel>();

        searchResultsViewModel.SaveSettings();
    }
}

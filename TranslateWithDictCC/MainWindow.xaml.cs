using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using TranslateWithDictCC.Views;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TranslateWithDictCC
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WinUIEx.WindowEx
    {
        public static MainWindow Instance { get; private set; }

        public UIElement ApplicationFrame => applicationFrame;

        public MainWindow(string launchArguments)
        {
            this.InitializeComponent();

            Instance = this;

            ResourceLoader resourceLoader = new ResourceLoader();

            Title = resourceLoader.GetString("App_Display_Name");

            applicationFrame.Navigate(typeof(MainPage), launchArguments);
        }
    }
}

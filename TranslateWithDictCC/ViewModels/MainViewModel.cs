using System.Windows.Input;

namespace TranslateWithDictCC.ViewModels
{
    class MainViewModel : ViewModel
    {
        public static readonly MainViewModel Instance = new MainViewModel();

        public ICommand NavigateToPageCommand { get; set; }
        public ICommand GoBackToPageCommand { get; set; }

        private MainViewModel()
        {
        }
    }
}

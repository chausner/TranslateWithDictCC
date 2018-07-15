using System;
using System.Reflection;
using System.Windows.Input;
using Windows.ApplicationModel.Resources;
using Windows.System;

namespace TranslateWithDictCC.ViewModels
{
    class AboutViewModel : ViewModel
    {
        public string AppVersion { get; }

        public ICommand GiveFeedbackCommand { get; }

        public AboutViewModel()
        {
            ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();
            
            AppVersion = string.Format(resourceLoader.GetString("AboutPage_Version"), GetType().GetTypeInfo().Assembly.GetName().Version);

            GiveFeedbackCommand = new RelayCommand(RunGiveFeedbackCommand);
        }

        private async void RunGiveFeedbackCommand()
        {
            await Launcher.LaunchUriAsync(new Uri("mailto:translatewithdict.cc@outlook.com"));
        }
    }
}

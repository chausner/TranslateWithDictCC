using TranslateWithDictCC.ViewModels;
using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace TranslateWithDictCC.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();

            DataContext = SettingsViewModel.Instance;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await SettingsViewModel.Instance.ImportDictionary();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DictionaryViewModel dictionary = (DictionaryViewModel)((Button)e.OriginalSource).DataContext;

            if (dictionary.Status == DictionaryStatus.Installed)
            {
                ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

                MessageDialog messageDialog = new MessageDialog(string.Format(
                    resourceLoader.GetString("Remove_Dictionary_Confirmation_Body"), 
                    dictionary.OriginLanguage, dictionary.DestinationLanguage));

                messageDialog.Commands.Add(new UICommand(resourceLoader.GetString("Remove_Dictionary_Confirmation_Remove")));
                messageDialog.Commands.Add(new UICommand(resourceLoader.GetString("Remove_Dictionary_Confirmation_Cancel")));

                messageDialog.DefaultCommandIndex = 0;
                messageDialog.CancelCommandIndex = 1;

                if (await messageDialog.ShowAsync() != messageDialog.Commands[0])
                    return;
            }

            await SettingsViewModel.Instance.RemoveDictionary(dictionary);
        }
    }
}

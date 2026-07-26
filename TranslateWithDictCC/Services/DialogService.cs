using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace TranslateWithDictCC.Services;

class DialogService
{
    public async Task<ContentDialogResult> ShowDialogAsync(ContentDialog contentDialog)
    {
        contentDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;
        return await contentDialog.ShowAsync();
    }
}

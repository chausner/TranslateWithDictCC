using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Settings;
using System.Threading;

namespace TranslateWithDictCC;

static class Program
{
    static void Main(string[] args)
    {
        XamlOptionalChanges.EnableChange(XamlChangeId.DefaultStyleOptimizations);
        XamlOptionalChanges.EnableChange(XamlChangeId.OptimizeApplyStyles);
        XamlOptionalChanges.EnableChange(XamlChangeId.IconNoGridOptimization);

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start((p) => {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }
}
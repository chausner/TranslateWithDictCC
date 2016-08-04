using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace TranslateWithDictCC
{
    static class ApplicationDataMigration
    {
        const uint Version = 1;

        public static async Task Migrate()
        {
            if (ApplicationData.Current.Version != Version)
                await ApplicationData.Current.SetVersionAsync(Version, SetVersionHandler);
        }

        private static async void SetVersionHandler(SetVersionRequest setVersionRequest)
        {
            SetVersionDeferral deferral = setVersionRequest.GetDeferral();

            if (setVersionRequest.CurrentVersion == setVersionRequest.DesiredVersion)
            {
            }
            else if (setVersionRequest.CurrentVersion == 0 && setVersionRequest.DesiredVersion == 1)
            {
            }
            else
                await ApplicationData.Current.ClearAsync();

            deferral.Complete();
        }
    }
}

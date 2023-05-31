using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace TranslateWithDictCC
{
    static class ApplicationDataMigration
    {
        const uint Version = 3;

        public static async Task Migrate()
        {
            if (ApplicationData.Current.Version != Version)
                await ApplicationData.Current.SetVersionAsync(Version, SetVersionHandler);
        }

        private static async void SetVersionHandler(SetVersionRequest setVersionRequest)
        {
            SetVersionDeferral deferral = setVersionRequest.GetDeferral();

            if (setVersionRequest.CurrentVersion > setVersionRequest.DesiredVersion)
            {
                // version of data is newer than our version. Normally, this should not happen.
                // Since downgrading is not supported, clear all application data.
                await ApplicationData.Current.ClearAsync();
            }
            else if (setVersionRequest.CurrentVersion == 0)
            {
                // app was newly installed, nothing to migrate
            }
            else
            {
                // migrations

                // remove left-over Dictionary entries that are not associated with a table
                if (setVersionRequest.CurrentVersion <= 1 && setVersionRequest.DesiredVersion > 1)
                {
                    await DatabaseManager.Instance.InitializeDb();

                    List<(int ID, string OriginLanguageCode, string DestinationLanguageCode)> dictionaries =
                        await DatabaseManager.Instance.ExecuteReader("SELECT * FROM Dictionaries", dataReader =>
                        {
                            int id = dataReader.GetInt32(0);
                            string originLanguageCode = dataReader.GetString(1);
                            string destinationLanguageCode = dataReader.GetString(2);
                            DateTimeOffset creationDate = new DateTimeOffset(dataReader.GetInt64(3), TimeSpan.Zero);
                            int numberOfEntries = dataReader.GetInt32(4);

                            return (id, originLanguageCode, destinationLanguageCode);
                        });

                    foreach (var dictionary in dictionaries)
                    {
                        string tableName = string.Format("Dictionary{0}{1}", dictionary.OriginLanguageCode, dictionary.DestinationLanguageCode);

                        object result = await DatabaseManager.Instance.ExecuteScalar($"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'");

                        if (result == null)
                            await DatabaseManager.Instance.ExecuteNonQuery($"DELETE FROM Dictionaries WHERE ID = {dictionary.ID}");
                    }
                }

                // add AppVersionWhenCreated column to the Dictionaries table
                if (setVersionRequest.CurrentVersion <= 2 && setVersionRequest.DesiredVersion > 2)
                    await DatabaseManager.Instance.ExecuteNonQuery("ALTER TABLE Dictionaries ADD COLUMN AppVersionWhenCreated VARCHAR(255)");
            }

            deferral.Complete();
        }
    }
}

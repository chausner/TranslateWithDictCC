using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace TranslateWithDictCC
{
    static class SubjectInfo
    {
        static bool loading;
        static JsonDocument subjectInfoRoot;

        public static async Task LoadAsync()
        {
            if (subjectInfoRoot != null || loading)
                return;

            try
            {
                loading = true;

                StorageFile storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Subjects.json"));

                using Stream stream = await storageFile.OpenStreamForReadAsync();

                subjectInfoRoot = await JsonDocument.ParseAsync(stream);
            }
            finally
            {
                loading = false;
            }
        }

        public static string GetSubjectDescription(string originLanguageCode, string destinationLanguageCode, string subject)
        {
            if (subjectInfoRoot == null)
                return null;

            if (!subjectInfoRoot.RootElement.TryGetProperty(originLanguageCode + destinationLanguageCode, out JsonElement subjectsOfLanguagePair) &&
                !subjectInfoRoot.RootElement.TryGetProperty(destinationLanguageCode + originLanguageCode, out subjectsOfLanguagePair))
                return null;

            if (!subjectsOfLanguagePair.TryGetProperty(subject, out JsonElement description))
                return null;

            return description.GetString();
        }
    }
}

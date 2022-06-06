using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace TranslateWithDictCC
{
    class SubjectInfo
    {
        public static SubjectInfo Instance { get; } = new SubjectInfo();

        JsonDocument subjectInfoRoot;

        private SubjectInfo()
        {
        }

        public async Task LoadAsync()
        {
            if (subjectInfoRoot != null)
                return;

            StorageFile storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Subjects.json"));

            using Stream stream = await storageFile.OpenStreamForReadAsync();

            subjectInfoRoot = await JsonDocument.ParseAsync(stream);
        }

        public string GetSubjectDescription(string originLanguageCode, string destinationLanguageCode, string subject)
        {
            JsonElement subjectsOfLanguagePair;

            if (!subjectInfoRoot.RootElement.TryGetProperty(originLanguageCode + destinationLanguageCode, out subjectsOfLanguagePair) &&
                !subjectInfoRoot.RootElement.TryGetProperty(destinationLanguageCode + originLanguageCode, out subjectsOfLanguagePair))
                return null;

            if (!subjectsOfLanguagePair.TryGetProperty(subject, out JsonElement description))
                return null;

            return description.GetString();
        }

        public bool IsLoaded => subjectInfoRoot != null;
    }
}

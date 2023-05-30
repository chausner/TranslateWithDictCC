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

        JsonDocument subjectInfoJson;

        private SubjectInfo()
        {
        }

        public async Task LoadAsync()
        {
            if (IsLoaded)
                return;

            StorageFile storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Subjects.json"));

            using Stream stream = await storageFile.OpenStreamForReadAsync();

            subjectInfoJson = await JsonDocument.ParseAsync(stream);
        }

        public string GetSubjectDescription(string originLanguageCode, string destinationLanguageCode, string subject)
        {
            if (!IsLoaded)
                throw new InvalidOperationException("Subjects have not been loaded yet");

            JsonElement subjectsOfLanguagePair;

            if (!subjectInfoJson.RootElement.TryGetProperty(originLanguageCode + destinationLanguageCode, out subjectsOfLanguagePair) &&
                !subjectInfoJson.RootElement.TryGetProperty(destinationLanguageCode + originLanguageCode, out subjectsOfLanguagePair))
                return null;

            if (!subjectsOfLanguagePair.TryGetProperty(subject, out JsonElement description))
                return null;

            return description.GetString();
        }

        public bool IsLoaded => subjectInfoJson != null;
    }
}

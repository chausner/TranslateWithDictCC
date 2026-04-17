using System;
using System.Collections.Frozen;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace TranslateWithDictCC;

class SubjectInfo
{
    public static SubjectInfo Instance { get; } = new SubjectInfo();

    FrozenDictionary<string, JsonElement>? subjects;

    private SubjectInfo()
    {
    }

    public async Task LoadAsync()
    {
        if (IsLoaded)
            return;

        StorageFile storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Subjects.json"));

        using Stream stream = await storageFile.OpenStreamForReadAsync();

        JsonDocument subjectsJson = await JsonDocument.ParseAsync(stream);

        subjects = subjectsJson!.RootElement.EnumerateArray().ToFrozenDictionary(jsonElement => jsonElement.GetProperty("EN").GetProperty("subject").GetString()!);
    }

    public (string LocalizedSubject, string Description)? LookupSubject(string originLanguageCode, string destinationLanguageCode, string subject)
    {
        if (!IsLoaded)
            throw new InvalidOperationException("Subjects have not been loaded yet");

        if (!subjects!.TryGetValue(subject, out JsonElement subjectInfo))
            return null;

        if (!subjectInfo.TryGetProperty(originLanguageCode, out JsonElement subjectInOriginLanguage))
            return null;

        if (!subjectInfo.TryGetProperty(destinationLanguageCode, out JsonElement subjectInDestinationLanguage))
            return null;

        string localizedSubject = subjectInOriginLanguage.GetProperty("subject").GetString()!;
        string originDescription = subjectInOriginLanguage.GetProperty("description").GetString()!;
        string destinationDescription = subjectInDestinationLanguage.GetProperty("description").GetString()!;
        string description = originDescription + " / " + destinationDescription;

        return (localizedSubject, description);
    }

    public bool IsLoaded => subjects != null;
}

using System;

namespace TranslateWithDictCC.Models;

class Dictionary
{
    public required int ID { get; set; }

    public required string OriginLanguageCode { get; set; }

    public required string DestinationLanguageCode { get; set; }

    public required DateTimeOffset CreationDate { get; set; }

    public required int NumberOfEntries { get; set; }

    public Version? AppVersionWhenCreated { get; set; }
}

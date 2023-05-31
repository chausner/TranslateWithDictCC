using System;

namespace TranslateWithDictCC.Models
{
    class Dictionary
    {
        public int ID { get; set; }

        public string OriginLanguageCode { get; set; }

        public string DestinationLanguageCode { get; set; }

        public DateTimeOffset CreationDate { get; set; }

        public int NumberOfEntries { get; set; }

        public Version AppVersionWhenCreated { get; set; }

        public Dictionary()
        {
        }

        public Dictionary(string originLanguageCode, string destinationLanguageCode, DateTimeOffset creationDate, int numberOfEntries, Version appVersionWhenCreated)
        {
            OriginLanguageCode = originLanguageCode;
            DestinationLanguageCode = destinationLanguageCode;
            CreationDate = creationDate;
            NumberOfEntries = numberOfEntries;
            AppVersionWhenCreated = appVersionWhenCreated;
        }
    }
}

using TranslateWithDictCC.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;

namespace TranslateWithDictCC
{
    class WordlistReader : IDisposable
    {
        StorageFile wordlistFile;
        StreamReader streamReader;
        ZipArchive zipArchive;
        long uncompressedSize;

        public string OriginLanguageCode { get; private set; }
        public string DestinationLanguageCode { get; private set; }
        public DateTimeOffset CreationDate { get; private set; }

        public WordlistReader(StorageFile wordlistFile)
        {
            this.wordlistFile = wordlistFile;
        }

        private async Task Open()
        {
            Stream stream = await wordlistFile.OpenStreamForReadAsync();

            if (wordlistFile.FileType.Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                zipArchive = new ZipArchive(stream, ZipArchiveMode.Read, false);

                ZipArchiveEntry wordlistFile = zipArchive.Entries.SingleOrDefault(
                    entry => Path.GetExtension(entry.FullName).Equals(".txt", StringComparison.OrdinalIgnoreCase));

                if (wordlistFile == null)
                    throw new InvalidDataException("Could not find a dict.cc wordlist file in the ZIP archive.");

                uncompressedSize = wordlistFile.Length;

                streamReader = new StreamReader(new StreamPositionWrapper(wordlistFile.Open()), Encoding.UTF8);
            }
            else
                streamReader = new StreamReader(stream, Encoding.UTF8);
        }

        private void Close()
        {
            if (streamReader != null)
            {
                streamReader.Dispose();
                streamReader = null;
            }
            if (zipArchive != null)
            {
                zipArchive.Dispose();
                zipArchive = null;
            }
        }

        public async Task ReadHeader()
        {
            await Open();

            string line = await streamReader.ReadLineAsync();

            Match match = Regex.Match(line, "^# ([A-Z]{2})-([A-Z]{2}) vocabulary database");

            if (!match.Success)
                throw new InvalidDataException("File is not recognized as a dict.cc wordlist file.");

            OriginLanguageCode = match.Groups[1].Value;
            DestinationLanguageCode = match.Groups[2].Value;

            line = await streamReader.ReadLineAsync();

            if (line.StartsWith("# Date and time\t") &&
                DateTime.TryParseExact(line.Substring(16), "yyyy-MM-dd HH\\:mm", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime creationDate))
            {
                TimeSpan offset = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time").GetUtcOffset(creationDate);
                CreationDate = new DateTimeOffset(creationDate, offset);
            }
            else
                CreationDate = DateTimeOffset.Now;
        }

        public async Task<IReadOnlyList<DictionaryEntry>> ReadEntries(int numEntries)
        {
            List<DictionaryEntry> entries = new List<DictionaryEntry>();

            while (entries.Count < numEntries && !streamReader.EndOfStream)
            {
                string line = await streamReader.ReadLineAsync();

                line = line.Trim(' ');

                if (line.StartsWith("#") || line.Length == 0)
                    continue;

                string[] s = line.Split('\t');

                if (s.Length < 3 || s.Length > 4)
                    continue;

                DictionaryEntry entry = new DictionaryEntry();

                entry.Word1 = WebUtility.HtmlDecode(s[0]);
                entry.Word2 = WebUtility.HtmlDecode(s[1]);

                if (s[2].Length != 0)
                    entry.WordClasses = s[2];

                entries.Add(entry);
            }

            return entries;
        }

        public double Progress
        {
            get
            {
                if (zipArchive == null)
                    return (double)streamReader.BaseStream.Position / streamReader.BaseStream.Length;
                else
                    return (double)streamReader.BaseStream.Position / uncompressedSize;
            }
        }

        private bool disposedValue = false; 

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (streamReader != null)
                    {
                        streamReader.Dispose();
                        streamReader = null;                        
                    }
                    if (zipArchive != null)
                    {
                        zipArchive.Dispose();
                        zipArchive = null;
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }    
}

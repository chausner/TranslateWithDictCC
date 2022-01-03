using TranslateWithDictCC.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace TranslateWithDictCC
{
    static class AudioRecordingFetcher
    {
        static Dictionary<LanguageWordPair, Uri> urlCache = new Dictionary<LanguageWordPair, Uri>();

        public static async Task<Uri> GetAudioRecordingUri(DictionaryEntryViewModel dictionaryEntryViewModel, bool word2)
        {
            DirectionViewModel selectedDirection = dictionaryEntryViewModel.SearchContext.SelectedDirection;

            string word = selectedDirection.ReverseSearch ^ word2 ? 
                dictionaryEntryViewModel.DictionaryEntry.Word2 : dictionaryEntryViewModel.DictionaryEntry.Word1;

            string otherWord = !selectedDirection.ReverseSearch ^ word2 ?
                dictionaryEntryViewModel.DictionaryEntry.Word2 : dictionaryEntryViewModel.DictionaryEntry.Word1;

            string originLanguageCode = word2 ? selectedDirection.DestinationLanguageCode : selectedDirection.OriginLanguageCode;
            string destinationLanguageCode = word2 ? selectedDirection.OriginLanguageCode : selectedDirection.DestinationLanguageCode;

            if (urlCache.TryGetValue(new LanguageWordPair(originLanguageCode, word), out Uri audioUri))
                return audioUri;

            Uri requestUri = GetSearchPageUri(originLanguageCode, destinationLanguageCode, word);

            string response;

            using (HttpClient httpClient = new HttpClient())
                response = await httpClient.GetStringAsync(requestUri);

            DictCCSearchResult[] dictCCSearchResults = ExtractDictCCSearchResults(response, originLanguageCode, destinationLanguageCode);

            string wordJSLiteral = GetJSLiteralOfWord(word);
            string otherWordJSLiteral = GetJSLiteralOfWord(otherWord);

            DictCCSearchResult dictCCSearchResult = null;

            if (wordJSLiteral != string.Empty || otherWordJSLiteral != string.Empty)
                dictCCSearchResult = dictCCSearchResults.FirstOrDefault(result => result.Word1 == wordJSLiteral && result.Word2 == otherWordJSLiteral);

            if (dictCCSearchResult != null)
                audioUri = GetAudioRecordingUri(dictCCSearchResult.ID, originLanguageCode, destinationLanguageCode, originLanguageCode);
            else
                audioUri = null;

            urlCache.Add(new LanguageWordPair(originLanguageCode, word), audioUri);

            return audioUri;
        }

        private static string GetJSLiteralOfWord(string word)
        {
            word = Regex.Replace(word, @" ?((\{.*?\})|(\[.*?\])|(\<.*?\>))", string.Empty).Trim();

            word = word.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"");

            return word;
        }

        private static Uri GetSearchPageUri(string originLanguageCode, string destinationLanguageCode, string searchQuery)
        {
            KeyValuePair<string, string>[] urlParameters = { new KeyValuePair<string, string>("s", searchQuery) };

            string encodedParameters = new HttpFormUrlEncodedContent(urlParameters).ReadAsStringAsync().AsTask().Result;

            return new Uri(string.Format("http://{0}-{1}.dict.cc/?{2}", originLanguageCode.ToLower(), destinationLanguageCode.ToLower(), encodedParameters));
        }

        private static Uri GetAudioRecordingUri(int id, string originLanguageCode, string destinationLanguageCode, string recordingLanguageCode)
        {
            KeyValuePair<string, string>[] urlParameters = {
                new KeyValuePair<string, string>("type", "mp3"),
                new KeyValuePair<string, string>("id", Convert.ToString(id)),
                new KeyValuePair<string, string>("lang", string.Format("{0}_rec_ip", recordingLanguageCode.ToLower())),
                new KeyValuePair<string, string>("lp", originLanguageCode.ToUpper() + destinationLanguageCode.ToUpper())
            };

            string encodedParameters = new HttpFormUrlEncodedContent(urlParameters).ReadAsStringAsync().AsTask().Result;

            return new Uri("http://audio.dict.cc/speak.audio.php?" + encodedParameters);
        }

        private static DictCCSearchResult[] ExtractDictCCSearchResults(string html, string originLanguageCode, string destinationLanguageCode)
        {
            Match match = Regex.Match(html, @"var idArr = new Array\((?:(\d+),?)*\);");

            if (!match.Success)
                return Array.Empty<DictCCSearchResult>();

            int[] ids = match.Groups[1].Captures.Cast<Capture>().Select(capture => Convert.ToInt32(capture.Value)).ToArray();

            match = Regex.Match(html, @"var c1Arr = new Array\((?:(""([^""]*)""),?)*\);");

            if (!match.Success)
                return Array.Empty<DictCCSearchResult>();

            string[] words1 = match.Groups[2].Captures.Cast<Capture>().Select(capture => capture.Value).ToArray();

            match = Regex.Match(html, @"var c2Arr = new Array\((?:(""([^""]*)""),?)*\);");

            if (!match.Success)
                return Array.Empty<DictCCSearchResult>();

            string[] words2 = match.Groups[2].Captures.Cast<Capture>().Select(capture => capture.Value).ToArray();

            if (ids.Length != words1.Length || ids.Length != words2.Length)
                return Array.Empty<DictCCSearchResult>();

            DictCCSearchResult[] results = new DictCCSearchResult[ids.Length];

            bool word2 = originLanguageCode == "DE" || (originLanguageCode == "EN" && destinationLanguageCode != "DE");

            for (int i = 0; i < ids.Length; i++)
                results[i] = new DictCCSearchResult
                {
                    ID = ids[i],
                    Word1 = word2 ? words2[i] : words1[i],
                    Word2 = word2 ? words1[i] : words2[i]
                };

            return results;
        }

        private record DictCCSearchResult
        {
            public int ID { get; init;  }
            public string Word1 { get; init; }
            public string Word2 { get; init; }
        }

        private record LanguageWordPair(string LanguageCode, string Word);
    }
}

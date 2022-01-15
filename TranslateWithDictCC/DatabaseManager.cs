using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TranslateWithDictCC.Models;
using TranslateWithDictCC.ViewModels;
using Windows.Storage;

namespace TranslateWithDictCC
{
    class DatabaseManager
    {
        public string DatabasePath { get; }

        public static readonly DatabaseManager Instance = new DatabaseManager(Path.Combine(ApplicationData.Current.LocalFolder.Path, "dictionaries.db"));

        bool initialized = false;

        private DatabaseManager(string databasePath)
        {
            DatabasePath = databasePath;
        }

        public async Task InitializeDb()
        {
            if (initialized)
                return;

            initialized = true;

            await ExecuteNonQuery("PRAGMA journal_mode=WAL");

            await ExecuteNonQuery("CREATE TABLE IF NOT EXISTS Dictionaries(ID INTEGER PRIMARY KEY NOT NULL, OriginLanguageCode VARCHAR(255) NOT NULL, DestinationLanguageCode VARCHAR(255) NOT NULL, CreationDate BIGINT NOT NULL, NumberOfEntries BIGINT NOT NULL)");
        }

        public async Task<DbConnection> OpenConnection()
        {
            DbConnection connection = SqliteFactory.Instance.CreateConnection();

            connection.ConnectionString = "Filename=" + DatabasePath;

            await connection.OpenAsync();

            return connection;
        }

        public async Task OpenTransactedConnection(Func<DbConnection, Task> action)
        {
            using (DbConnection connection = await OpenConnection())
            using (DbTransaction transaction = connection.BeginTransaction())
            {
                try
                {
                    await action(connection);

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public async Task<int> ExecuteNonQuery(string commandText)
        {
            using (DbConnection connection = await OpenConnection())
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = commandText;

                return await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<object> ExecuteScalar(string commandText)
        {
            using (DbConnection connection = await OpenConnection())
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = commandText;

                return await command.ExecuteScalarAsync();
            }
        }

        public async Task<List<T>> ExecuteReader<T>(string commandText, Func<DbDataReader, T> dataReaderFunc)
        {
            List<T> results = new List<T>();

            using (DbConnection connection = await OpenConnection())
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = commandText;

                using (DbDataReader dataReader = await command.ExecuteReaderAsync())
                    while (await dataReader.ReadAsync())
                    {
                        T result = dataReaderFunc(dataReader);
                        results.Add(result);
                    }
            }

            return results;
        }

        public Task<List<Dictionary>> GetDictionaries()
        {
            return ExecuteReader("SELECT * FROM Dictionaries", dataReader =>
            {
                Dictionary dictionary = new Dictionary();
                
                dictionary.ID = dataReader.GetInt32(0);
                dictionary.OriginLanguageCode = dataReader.GetString(1);
                dictionary.DestinationLanguageCode = dataReader.GetString(2);
                dictionary.CreationDate = new DateTimeOffset(dataReader.GetInt64(3), TimeSpan.Zero);
                dictionary.NumberOfEntries = dataReader.GetInt32(4);

                return dictionary;
            });
        }

        private string GetDictionaryTableName(string originLanguageCode, string destinationLanguageCode)
        {
            return string.Format("Dictionary{0}{1}", originLanguageCode, destinationLanguageCode);
        }

        public async Task<Dictionary> ImportWordlist(DictionaryViewModel dictionaryViewModel, CancellationToken cancellationToken)
        {
            Dictionary dictionary = null;

            await OpenTransactedConnection(async connection =>
            {
                WordlistReader wordlistReader = dictionaryViewModel.WordlistReader;

                string tableName = GetDictionaryTableName(wordlistReader.OriginLanguageCode, wordlistReader.DestinationLanguageCode);

                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = "CREATE VIRTUAL TABLE " + tableName + " USING fts4(Word1 VARCHAR NOT NULL, Word2 VARCHAR NOT NULL, WordClasses VARCHAR, tokenize=unicode61)";

                    await command.ExecuteNonQueryAsync();
                }

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    IReadOnlyList<DictionaryEntry> entries = await wordlistReader.ReadEntries(1000);

                    if (entries.Count == 0)
                        break;

                    // run on the thread pool for better UI responsiveness
                    await Task.Run(delegate ()
                    {
                        using (DbCommand command = connection.CreateCommand())
                        {
                            command.CommandText = $"INSERT INTO {tableName}(Word1, Word2, WordClasses) VALUES (@Word1, @Word2, @WordClasses)";

                            command.Parameters.Add(new SqliteParameter("@Word1", SqliteType.Text, 512));
                            command.Parameters.Add(new SqliteParameter("@Word2", SqliteType.Text, 512));
                            command.Parameters.Add(new SqliteParameter("@WordClasses", SqliteType.Text, 64));

                            command.Prepare();

                            foreach (DictionaryEntry entry in entries)
                            {
                                command.Parameters[0].Value = entry.Word1;
                                command.Parameters[1].Value = entry.Word2;
                                command.Parameters[2].Value = (object)entry.WordClasses ?? DBNull.Value;

                                command.ExecuteNonQuery();
                            }
                        }
                    });

                    dictionaryViewModel.NumberOfEntries += entries.Count;
                    dictionaryViewModel.ImportProgress = wordlistReader.Progress;
                }

                dictionaryViewModel.ImportProgress = 1.0;

                dictionary = new Dictionary(wordlistReader.OriginLanguageCode, wordlistReader.DestinationLanguageCode, wordlistReader.CreationDate, dictionaryViewModel.NumberOfEntries);

                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO Dictionaries(OriginLanguageCode, DestinationLanguageCode, CreationDate, NumberOfEntries) VALUES (@OriginLanguageCode, @DestinationLanguageCode, @CreationDate, @NumberOfEntries)";

                    command.Parameters.Add(new SqliteParameter("@OriginLanguageCode", dictionary.OriginLanguageCode));
                    command.Parameters.Add(new SqliteParameter("@DestinationLanguageCode", dictionary.DestinationLanguageCode));
                    command.Parameters.Add(new SqliteParameter("@CreationDate", dictionary.CreationDate.UtcTicks));
                    command.Parameters.Add(new SqliteParameter("@NumberOfEntries", dictionary.NumberOfEntries));

                    await command.ExecuteNonQueryAsync();
                }

                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT last_insert_rowid()";

                    dictionary.ID = (int)(long)await command.ExecuteScalarAsync();
                }

                dictionaryViewModel.Dictionary = dictionary;
            });

            return dictionary;
        }

        public Task<List<DictionaryEntry>> QueryEntries(Dictionary dictionary, string searchQuery, bool reverseSearch)
        {
            return QueryEntries(dictionary, searchQuery, reverseSearch, -1);
        }

        public Task<List<DictionaryEntry>> QueryEntries(Dictionary dictionary, string searchQuery, bool reverseSearch, int maxResults)
        {
            string tableName = GetDictionaryTableName(dictionary.OriginLanguageCode, dictionary.DestinationLanguageCode);

            string column = reverseSearch ? "Word2" : "Word1";

            string commandText = string.Format("SELECT *, offsets({0}) FROM {0} WHERE {1} MATCH '\"{2}\"'", tableName, column, SqlEscapeString(searchQuery));

            if (maxResults >= 0)
                commandText += " LIMIT " + maxResults;

            return ExecuteReader(commandText, dataReader =>
            {
                string word1 = dataReader.GetString(0);
                string word2 = dataReader.GetString(1);
                string wordClasses = dataReader.GetValue(2) as string;
                string[] offsets = dataReader.GetString(3).Split(' ');

                TextSpan[] matchSpans = new TextSpan[offsets.Length / 4];

                string word = reverseSearch ? word2 : word1;

                int[] byteCounts = null;

                if (Encoding.UTF8.GetByteCount(word) != word.Length)
                {
                    byteCounts = new int[word.Length + 1];

                    char[] wordChars = word.ToCharArray();

                    for (int i = 0; i < word.Length; i++)
                        byteCounts[i + 1] = byteCounts[i] + Encoding.UTF8.GetByteCount(wordChars, i, 1);
                }

                for (int i = 0; i < matchSpans.Length; i++)
                {
                    int offset = Convert.ToInt32(offsets[4 * i + 2]);
                    int length = Convert.ToInt32(offsets[4 * i + 3]);

                    if (byteCounts != null)
                    {
                        int adjustedOffset = Array.BinarySearch(byteCounts, offset);
                        int adjustedLength = Array.BinarySearch(byteCounts, offset + length) - adjustedOffset;
                        offset = adjustedOffset;
                        length = adjustedLength;
                    }

                    matchSpans[i] = new TextSpan(offset, length);
                }

                return new DictionaryEntry { Word1 = word1, Word2 = word2, WordClasses = wordClasses, MatchSpans = matchSpans };
            });
        }

        public Task OptimizeTable(Dictionary dictionary)
        {
            string tableName = GetDictionaryTableName(dictionary.OriginLanguageCode, dictionary.DestinationLanguageCode);

            return ExecuteNonQuery(string.Format("INSERT INTO {0}({0}) VALUES('optimize');", tableName));
        }

        private static string SqlEscapeString(string s)
        {
            return s.Replace("'", "''").Replace("\"", "\"\"");
        }

        public Task DeleteDictionary(Dictionary dictionary)
        {
            return OpenTransactedConnection(async connection =>
            {
                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"DELETE FROM Dictionaries WHERE ID = {dictionary.ID}";

                    await command.ExecuteNonQueryAsync();
                }

                using (DbCommand command = connection.CreateCommand())
                {
                    string tableName = GetDictionaryTableName(dictionary.OriginLanguageCode, dictionary.DestinationLanguageCode);

                    command.CommandText = $"DROP TABLE {tableName}";

                    await command.ExecuteNonQueryAsync();
                }
            });
        }
    }
}

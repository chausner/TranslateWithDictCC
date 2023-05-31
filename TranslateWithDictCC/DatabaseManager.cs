using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Reflection;
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

            await ExecuteNonQuery(
                @"CREATE TABLE IF NOT EXISTS Dictionaries(
                      ID INTEGER PRIMARY KEY NOT NULL,
                      OriginLanguageCode VARCHAR(255) NOT NULL,
                      DestinationLanguageCode VARCHAR(255) NOT NULL,
                      CreationDate BIGINT NOT NULL,
                      NumberOfEntries BIGINT NOT NULL,
                      AppVersionWhenCreated VARCHAR(255))");
        }

        public async Task<DbConnection> OpenConnection()
        {
            DbConnection connection = SqliteFactory.Instance.CreateConnection();

            SqliteConnectionStringBuilder builder = new SqliteConnectionStringBuilder()
            {
                DataSource = DatabasePath
            };

            connection.ConnectionString = builder.ToString();

            await connection.OpenAsync();

            return connection;
        }

        public async Task OpenTransactedConnection(Func<DbConnection, Task> action)
        {
            await using DbConnection connection = await OpenConnection();
            await using DbTransaction transaction = connection.BeginTransaction();

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

        public async Task<T> OpenTransactedConnection<T>(Func<DbConnection, Task<T>> action)
        {
            await using DbConnection connection = await OpenConnection();
            await using DbTransaction transaction = connection.BeginTransaction();

            T result;

            try
            {
                result = await action(connection);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            return result;
        }

        public async Task<int> ExecuteNonQuery(string commandText)
        {
            await using DbConnection connection = await OpenConnection();
            await using DbCommand command = connection.CreateCommand();

            command.CommandText = commandText;

            return await command.ExecuteNonQueryAsync();
        }

        public async Task<object> ExecuteScalar(string commandText)
        {
            await using DbConnection connection = await OpenConnection();
            await using DbCommand command = connection.CreateCommand();

            command.CommandText = commandText;

            return await command.ExecuteScalarAsync();
        }

        public Task<List<T>> ExecuteReader<T>(string commandText, Func<DbDataReader, T> dataReaderFunc)
        {
            return ExecuteReader(command => { command.CommandText = commandText; }, dataReaderFunc);
        }

        public async Task<List<T>> ExecuteReader<T>(Action<DbCommand> commandFunc, Func<DbDataReader, T> dataReaderFunc)
        {
            await using DbConnection connection = await OpenConnection();
            await using DbCommand command = connection.CreateCommand();
            
            commandFunc(command);

            await using DbDataReader dataReader = await command.ExecuteReaderAsync();

            List<T> results = new List<T>();

            while (await dataReader.ReadAsync())
            {
                T result = dataReaderFunc(dataReader);
                results.Add(result);
            }            

            return results;
        }

        public async Task<List<Dictionary>> GetDictionaries()
        {
            return await ExecuteReader("SELECT * FROM Dictionaries", dataReader =>
            {
                return new Dictionary
                {
                    ID = dataReader.GetInt32(0),
                    OriginLanguageCode = dataReader.GetString(1),
                    DestinationLanguageCode = dataReader.GetString(2),
                    CreationDate = new DateTimeOffset(dataReader.GetInt64(3), TimeSpan.Zero),
                    NumberOfEntries = dataReader.GetInt32(4),
                    AppVersionWhenCreated = dataReader.IsDBNull(5) ? null : Version.Parse(dataReader.GetString(5))
                };
            });
        }

        private static string GetDictionaryTableName(string originLanguageCode, string destinationLanguageCode)
        {
            return "Dictionary" + originLanguageCode + destinationLanguageCode;
        }

        public async Task<Dictionary> ImportWordlist(DictionaryViewModel dictionaryViewModel, CancellationToken cancellationToken)
        {
            return await OpenTransactedConnection(async connection =>
            {
                WordlistReader wordlistReader = dictionaryViewModel.WordlistReader;

                string tableName = GetDictionaryTableName(wordlistReader.OriginLanguageCode, wordlistReader.DestinationLanguageCode);

                await using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"CREATE VIRTUAL TABLE {tableName} USING fts4(Word1 VARCHAR NOT NULL, Word2 VARCHAR NOT NULL, WordClasses VARCHAR, Subjects VARCHAR, tokenize=unicode61, notindexed=WordClasses, notindexed=Subjects)";

                    await command.ExecuteNonQueryAsync();
                }

                await using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"INSERT INTO {tableName}(Word1, Word2, WordClasses, Subjects) VALUES (@Word1, @Word2, @WordClasses, @Subjects)";

                    command.Parameters.Add(new SqliteParameter("@Word1", SqliteType.Text, 512));
                    command.Parameters.Add(new SqliteParameter("@Word2", SqliteType.Text, 512));
                    command.Parameters.Add(new SqliteParameter("@WordClasses", SqliteType.Text, 64));
                    command.Parameters.Add(new SqliteParameter("@Subjects", SqliteType.Text, 128));

                    await command.PrepareAsync();

                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        IReadOnlyList<DictionaryEntry> entries = await wordlistReader.ReadEntries(2500);

                        if (entries.Count == 0)
                            break;

                        // run on the thread pool for better UI responsiveness
                        await Task.Run(delegate ()
                        {
                            foreach (DictionaryEntry entry in entries)
                            {
                                command.Parameters[0].Value = entry.Word1;
                                command.Parameters[1].Value = entry.Word2;
                                command.Parameters[2].Value = (object)entry.WordClasses ?? DBNull.Value;
                                command.Parameters[3].Value = (object)entry.Subjects ?? DBNull.Value;

                                command.ExecuteNonQuery();
                            }
                        });

                        dictionaryViewModel.NumberOfEntries += entries.Count;
                        dictionaryViewModel.ImportProgress = wordlistReader.Progress;
                    }
                }

                dictionaryViewModel.ImportProgress = 1.0;

                Version appVersion = GetType().GetTypeInfo().Assembly.GetName().Version;

                Dictionary dictionary = new Dictionary(
                    wordlistReader.OriginLanguageCode,
                    wordlistReader.DestinationLanguageCode,
                    wordlistReader.CreationDate,
                    dictionaryViewModel.NumberOfEntries,
                    appVersion);

                await using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO Dictionaries(OriginLanguageCode, DestinationLanguageCode, CreationDate, NumberOfEntries, AppVersionWhenCreated) VALUES (@OriginLanguageCode, @DestinationLanguageCode, @CreationDate, @NumberOfEntries, @AppVersionWhenCreated)";

                    command.Parameters.Add(new SqliteParameter("@OriginLanguageCode", dictionary.OriginLanguageCode));
                    command.Parameters.Add(new SqliteParameter("@DestinationLanguageCode", dictionary.DestinationLanguageCode));
                    command.Parameters.Add(new SqliteParameter("@CreationDate", dictionary.CreationDate.UtcTicks));
                    command.Parameters.Add(new SqliteParameter("@NumberOfEntries", dictionary.NumberOfEntries));
                    command.Parameters.Add(new SqliteParameter("@AppVersionWhenCreated", (object)dictionary.AppVersionWhenCreated?.ToString() ?? DBNull.Value));

                    await command.ExecuteNonQueryAsync();
                }

                await using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT last_insert_rowid()";

                    dictionary.ID = (int)(long)await command.ExecuteScalarAsync();
                }

                dictionaryViewModel.Dictionary = dictionary;

                return dictionary;
            });
        }

        public Task<List<DictionaryEntry>> QueryEntries(Dictionary dictionary, string searchQuery, bool reverseSearch)
        {
            return QueryEntries(dictionary, searchQuery, reverseSearch, -1);
        }

        public async Task<List<DictionaryEntry>> QueryEntries(Dictionary dictionary, string searchQuery, bool reverseSearch, int maxResults)
        {
            string tableName = GetDictionaryTableName(dictionary.OriginLanguageCode, dictionary.DestinationLanguageCode);
            string column = reverseSearch ? "Word2" : "Word1";
            string escapedQuery = '\"' + searchQuery.Replace("\"", "\"\"") + '\"';

            bool hasSubjectsColumn = dictionary.AppVersionWhenCreated != null &&
                dictionary.AppVersionWhenCreated >= Version.Parse("2.1.0");

            return await ExecuteReader(command =>
            {
                command.CommandText = $"SELECT *, offsets({tableName}) FROM {tableName} WHERE {column} MATCH @Query";

                if (maxResults >= 0)
                    command.CommandText += " LIMIT " + maxResults;

                command.Parameters.Add(new SqliteParameter("@Query", escapedQuery));
            }, dataReader =>
            {
                string word1 = dataReader.GetString(0);
                string word2 = dataReader.GetString(1);
                string wordClasses = dataReader.GetValue(2) as string;
                string subjects;
                string[] offsets;

                if (!hasSubjectsColumn)
                {
                    // old table format without Subjects column
                    subjects = null;
                    offsets = dataReader.GetString(3).Split(' ');
                }
                else
                {
                    subjects = dataReader.GetValue(3) as string;
                    offsets = dataReader.GetString(4).Split(' ');
                }

                TextSpan[] matchSpans = GetMatchSpans(reverseSearch ? word2 : word1, offsets);

                return new DictionaryEntry { Word1 = word1, Word2 = word2, WordClasses = wordClasses, Subjects = subjects, MatchSpans = matchSpans };
            });
        }

        private static TextSpan[] GetMatchSpans(string word, string[] offsets)
        {
            TextSpan[] matchSpans = new TextSpan[offsets.Length / 4];

            int[] byteCounts = null;

            if (Encoding.UTF8.GetByteCount(word) != word.Length)
            {
                byteCounts = new int[word.Length + 1];

                for (int i = 0; i < word.Length; i++)
                    byteCounts[i + 1] = byteCounts[i] + Encoding.UTF8.GetByteCount(word, i, 1);
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

            return matchSpans;
        }

        public async Task OptimizeTable(Dictionary dictionary)
        {
            // run on the thread pool for better UI responsiveness
            await Task.Run(async delegate ()
            {
                string tableName = GetDictionaryTableName(dictionary.OriginLanguageCode, dictionary.DestinationLanguageCode);

                await ExecuteNonQuery($"INSERT INTO {tableName}({tableName}) VALUES('optimize');");
            });
        }

        public async Task DeleteDictionary(Dictionary dictionary)
        {
            // run on the thread pool for better UI responsiveness
            await Task.Run(async delegate ()
            {
                await OpenTransactedConnection(async connection =>
                {
                    await using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"DELETE FROM Dictionaries WHERE ID = {dictionary.ID}";

                        await command.ExecuteNonQueryAsync();
                    }

                    await using (DbCommand command = connection.CreateCommand())
                    {
                        string tableName = GetDictionaryTableName(dictionary.OriginLanguageCode, dictionary.DestinationLanguageCode);

                        command.CommandText = $"DROP TABLE {tableName}";

                        await command.ExecuteNonQueryAsync();
                    }
                });
            });
        }
    }
}

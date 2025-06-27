using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Text.RegularExpressions;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Store;
using Lucene.Net.Util;
using WikiApp.Data.Model;



namespace WikiApp.Services
{
    internal class NoteService
    {
        private const string DbPath = "notes.db";

        public static List<CategoryModel> GetCategories()
        {
            var categories = new List<CategoryModel>();

            using var conn = new SQLiteConnection($"Data Source={DbPath}");
            conn.Open();

            var cmd = new SQLiteCommand("SELECT * FROM Categories WHERE IsDeleted = 0", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                categories.Add(new CategoryModel
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    IsDeleted = reader.GetInt32(2) == 1
                });
            }

            return categories;
        }

        public static List<NoteModel> GetNotesByCategory(int categoryId)
        {
            var notes = new List<NoteModel>();

            using var conn = new SQLiteConnection($"Data Source={DbPath}");
            conn.Open();

            var cmd = new SQLiteCommand("SELECT * FROM Notes WHERE CategoryId = @id AND IsDeleted = 0", conn);
            cmd.Parameters.AddWithValue("@id", categoryId);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                notes.Add(new NoteModel
                {
                    Id = reader.GetInt32(0),
                    CategoryId = reader.GetInt32(1),
                    Title = reader.GetString(2),
                    FilePath = reader.GetString(3),
                    IsDeleted = reader.GetInt32(4) == 1
                });
            }

            return notes;
        }

        public static int CreateCategory(string name)
        {
            try
            {
                using var conn = new SQLiteConnection("Data Source=notes.db");
                conn.Open();

                var cmd = new SQLiteCommand(
                    "INSERT INTO Categories (Name, IsDeleted) VALUES (@name, 0); SELECT last_insert_rowid();", conn);

                cmd.Parameters.AddWithValue("@name", name);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                AppLogger.Log(ex);
                return -1;
            }
        }

        public static void UpdateCategory(int id, string newName)
        {
            try
            {
                using var conn = new SQLiteConnection("Data Source=notes.db");
                conn.Open();

                var cmd = new SQLiteCommand("UPDATE Categories SET Name = @name WHERE Id = @id", conn);
                cmd.Parameters.AddWithValue("@name", newName);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                AppLogger.Log(ex);
            }
        }

        public static int CreateNote(int categoryId, string title, string filePath)
        {
            try
            {
                using var conn = new SQLiteConnection("Data Source=notes.db");
                conn.Open();

                var cmd = new SQLiteCommand(
                    "INSERT INTO Notes (CategoryId, Title, FilePath, IsDeleted) VALUES (@categoryId, @title, @filePath, 0); " +
                    "SELECT last_insert_rowid();", conn);

                cmd.Parameters.AddWithValue("@categoryId", categoryId);
                cmd.Parameters.AddWithValue("@title", title);
                cmd.Parameters.AddWithValue("@filePath", filePath);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                AppLogger.Log(ex);
                return -1;
            }
        }

        public static void DeleteCategory(int categoryId)
        {
            try
            {
                using var conn = new SQLiteConnection("Data Source=notes.db");
                conn.Open();

                var cmd = new SQLiteCommand("UPDATE Categories SET IsDeleted = 1 WHERE Id = @id", conn);
                cmd.Parameters.AddWithValue("@id", categoryId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                AppLogger.Log(ex);
            }
        }

        public static void DeleteNote(int noteId)
        {
            try
            {
                using var conn = new SQLiteConnection("Data Source=notes.db");
                conn.Open();

                var cmd = new SQLiteCommand("UPDATE Notes SET IsDeleted = 1 WHERE Id = @id", conn);
                cmd.Parameters.AddWithValue("@id", noteId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                AppLogger.Log(ex);
            }
        }

        public static List<string> GetTagsForNote(int noteId)
        {
            var tags = new List<string>();
            using var conn = new SQLiteConnection("Data Source=notes.db");
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT t.Name FROM Tags t
                        INNER JOIN NoteTags nt ON t.Id = nt.TagId
                        WHERE nt.NoteId = @noteId;";
            cmd.Parameters.AddWithValue("@noteId", noteId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                tags.Add(reader.GetString(0));
            }

            return tags;
        }

        public static void UpdateTagsForNote(int noteId, List<string> tags)
        {
            using var conn = new SQLiteConnection("Data Source=notes.db");
            conn.Open();

            using var tx = conn.BeginTransaction();

            var deleteCmd = conn.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM NoteTags WHERE NoteId = @noteId;";
            deleteCmd.Parameters.AddWithValue("@noteId", noteId);
            deleteCmd.ExecuteNonQuery();

            foreach (var tag in tags.Distinct())
            {
                var insertTag = conn.CreateCommand();
                insertTag.CommandText = "INSERT OR IGNORE INTO Tags (Name) VALUES (@tag);";
                insertTag.Parameters.AddWithValue("@tag", tag);
                insertTag.ExecuteNonQuery();

                var getTagId = conn.CreateCommand();
                getTagId.CommandText = "SELECT Id FROM Tags WHERE Name = @tag;";
                getTagId.Parameters.AddWithValue("@tag", tag);
                var tagId = Convert.ToInt32(getTagId.ExecuteScalar());

                var linkCmd = conn.CreateCommand();
                linkCmd.CommandText = "INSERT INTO NoteTags (NoteId, TagId) VALUES (@noteId, @tagId);";
                linkCmd.Parameters.AddWithValue("@noteId", noteId);
                linkCmd.Parameters.AddWithValue("@tagId", tagId);
                linkCmd.ExecuteNonQuery();
            }

            tx.Commit();
        }



        private readonly string indexPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LuceneIndex");
        private readonly LuceneVersion luceneVersion = LuceneVersion.LUCENE_48;

        public List<NoteSearchResult> SearchNotes(string query)
        {
            var results = new List<NoteSearchResult>();

            if (!System.IO.Directory.Exists(indexPath))
                return results;

            var dir = FSDirectory.Open(indexPath);
            if (!DirectoryReader.IndexExists(dir))
                return results;

            using var reader = DirectoryReader.Open(dir);
            var searcher = new IndexSearcher(reader);
            var analyzer = new StandardAnalyzer(luceneVersion);

            var wildcardQuery = new WildcardQuery(new Term("Content", $"*{query.ToLower()}*"));

            var hits = searcher.Search(wildcardQuery, 100).ScoreDocs;

            foreach (var hit in hits)
            {
                var doc = searcher.Doc(hit.Doc);
                var content = doc.Get("Content") ?? string.Empty;

                string highlightedSnippet = GenerateSnippet(content, query);

                results.Add(new NoteSearchResult
                {
                    FileName = doc.Get("FileName") ?? string.Empty,
                    FullPath = doc.Get("FullPath") ?? string.Empty,
                    Snippet = highlightedSnippet,
                    MatchStartIndex = content.IndexOf(query, StringComparison.OrdinalIgnoreCase),
                    HighlightedKeyword = query
                });
            }

            return results;
        }

        private string GenerateSnippet(string content, string keyword)
        {
            if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(keyword))
                return "";

            // Escape for regex
            string escapedKeyword = Regex.Escape(keyword);
            string pattern = $"(?i)\\b({escapedKeyword})\\b"; // Match whole word (optional)

            // Only highlight inside the snippet
            int previewStart = Math.Max(content.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) - 50, 0);
            int previewLength = Math.Min(300, content.Length - previewStart);
            string snippet = content.Substring(previewStart, previewLength);

            return Regex.Replace(snippet, pattern, "<mark>$1</mark>", RegexOptions.IgnoreCase);
        }




        public void BuildIndex(IEnumerable<NoteModel> notes)
        {
            var dir = FSDirectory.Open(indexPath);
            var analyzer = new StandardAnalyzer(luceneVersion);
            var config = new IndexWriterConfig(luceneVersion, analyzer);

            using var writer = new IndexWriter(dir, config);

            foreach (var note in notes)
            {
                if (File.Exists(note.FilePath))
                {
                    var content = File.ReadAllText(note.FilePath);
                    var doc = new Lucene.Net.Documents.Document
                    {
                        new Lucene.Net.Documents.StringField("FileName", Path.GetFileName(note.FilePath), Lucene.Net.Documents.Field.Store.YES),
                        new Lucene.Net.Documents.StringField("FullPath", note.FilePath, Lucene.Net.Documents.Field.Store.YES),
                        new Lucene.Net.Documents.TextField("Content", content, Lucene.Net.Documents.Field.Store.YES)
                    };

                    writer.AddDocument(doc);
                }
            }

            writer.Commit();
        }




    }
}

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WikiApp.Services;
using System.IO;

namespace WikiApp.Data.Access
{
    public static class DatabaseHelper
    {
        private static readonly string DbPath = "notes.db";
        public static void InitializeDatabase()
        {
            try
            {
                if (!File.Exists(DbPath))
                    SQLiteConnection.CreateFile(DbPath);

                using var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;");
                conn.Open();

                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Categories (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        IsDeleted INTEGER DEFAULT 0
                    );

                    CREATE TABLE IF NOT EXISTS Notes (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        CategoryId INTEGER,
                        Title TEXT NOT NULL,
                        FilePath TEXT NOT NULL,
                        IsDeleted INTEGER DEFAULT 0,
                        FOREIGN KEY(CategoryId) REFERENCES Categories(Id)
                    );

                    CREATE TABLE IF NOT EXISTS Tags (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL UNIQUE
                    );

                    CREATE TABLE IF NOT EXISTS NoteTags (
                        NoteId INTEGER NOT NULL,
                        TagId INTEGER NOT NULL,
                        FOREIGN KEY(NoteId) REFERENCES Notes(Id),
                        FOREIGN KEY(TagId) REFERENCES Tags(Id),
                        PRIMARY KEY (NoteId, TagId)
                    );";
  
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                AppLogger.Log(ex);
            }
        }

        public static DataTable GetActiveCategoriesWithNotes()
        {
            try
            {
                using var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;");
                conn.Open();

                var adapter = new SQLiteDataAdapter(@"
                    SELECT c.Id as CategoryId, c.Name as CategoryName, 
                           n.Id as NoteId, n.Title as NoteTitle, n.FilePath
                    FROM Categories c
                    LEFT JOIN Notes n ON c.Id = n.CategoryId AND n.IsDeleted = 0
                    WHERE c.IsDeleted = 0
                    ORDER BY c.Name, n.Title;", conn);

                var table = new DataTable();
                adapter.Fill(table);
                return table;
            }
            catch (Exception ex)
            {
                AppLogger.Log(ex);
                return null;
            }
        }
    }
}

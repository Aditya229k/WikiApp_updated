using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikiApp.Services
{
    public static class AppLogger
    {
        private static readonly string LogFilePath;

        static AppLogger()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WikiApp");

            if (!Directory.Exists(appDataPath))
                Directory.CreateDirectory(appDataPath);

            LogFilePath = Path.Combine(appDataPath, "wikiapp.log");
        }

        public static void Log(Exception ex)
        {
            try
            {
                string message = $"[{DateTime.Now}] {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}";
                File.AppendAllText(LogFilePath, message);
            }
            catch
            {
                // Silently fail — logging should not crash the app
            }
        }

        public static void Log(string customMessage)
        {
            try
            {
                string message = $"[{DateTime.Now}] {customMessage}{Environment.NewLine}";
                File.AppendAllText(LogFilePath, message);
            }
            catch
            {
                // Silently fail
            }
        }
    }
}

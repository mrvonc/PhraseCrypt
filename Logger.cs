using System;
using System.IO;
using System.Text;

namespace PhraseCryptApp
{
    /// <summary>
    /// Simple file logger. IMPORTANT: plaintext hex, mnemonic words, passwords and
    /// encrypted payloads are deliberately never logged. Only actions and metadata
    /// are recorded (for example "12 words generated"), so that the log file itself
    /// never becomes a security risk.
    /// </summary>
    public static class Logger
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PhraseCryptApp");

        private static readonly string LogFilePath = Path.Combine(LogDirectory, "phrasecrypt.log");
        private static readonly object LockObj = new object();

        public static string CurrentLogFilePath => LogFilePath;

        public static void Info(string message) => Write("INFO", message);
        public static void Warn(string message) => Write("WARN", message);
        public static void Error(string message) => Write("ERROR", message);

        private static void Write(string level, string message)
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
                string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}";

                lock (LockObj)
                {
                    File.AppendAllText(LogFilePath, line, Encoding.UTF8);
                }
            }
            catch
            {
                // Logging must never be able to crash the application.
            }
        }
    }
}

using System;
using System.IO;
using System.Text.Json;

namespace EasyLog
{
    public class JsonLogger : ILogger
    {
        private readonly string logDirectory;
        private string currentLogFile;

        public JsonLogger(string logDirectory)
        {
            this.logDirectory = string.IsNullOrWhiteSpace(logDirectory)
                ? Path.GetFullPath(".")
                : logDirectory;

            this.currentLogFile = string.Empty;

            Directory.CreateDirectory(this.logDirectory);
            CreateDailyLogFile();
        }

        public void LogAction(LogEntry entry)
        {
            if (entry == null) return;

            try
            {
                CreateDailyLogFile();
                // Normalize paths to absolute/UNC-style before serializing
                try
                {
                    entry.SourceFilePath = entry.ToUNCPath(entry.SourceFilePath);
                }
                catch { }

                try
                {
                    entry.TargetFilePath = entry.ToUNCPath(entry.TargetFilePath);
                }
                catch { }

                var json = SerializeToJson(entry);
                var path = Path.Combine(logDirectory, currentLogFile);

                File.AppendAllText(path, json + Environment.NewLine);
            }
            catch
            {
            }
        }

        public void CreateDailyLogFile()
        {
            var fileName = DateTime.UtcNow.ToString("yyyy-MM-dd") + ".log.json";
            currentLogFile = fileName;

            var path = Path.Combine(logDirectory, currentLogFile);
            if (!File.Exists(path))
            {
                try
                {
                    using var fs = File.Create(path);
                }
                catch
                {
                }
            }
        }

        public string SerializeToJson(LogEntry entry)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            try
            {
                return JsonSerializer.Serialize(entry, options);
            }
            catch
            {
                return "{}";
            }
        }
    }
}

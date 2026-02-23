using System;
using System.IO;
using System.Text.Json;

namespace EasyLog
{
    public class JsonLogger : ILogger
    {
        private readonly string logDirectory;
        private string currentLogFile;
        // Lock object for thread-safe file writing in parallel execution
        private readonly object _lockObject = new object();
        public LogFormat Format => LogFormat.Json;

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
                lock (_lockObject)
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

                    string json = SerializeToJson(entry);
                    string path = Path.Combine(logDirectory, currentLogFile);

                    File.AppendAllText(path, json + Environment.NewLine);
                }
            }
            catch
            {
            }
        }

        public void CreateDailyLogFile()
        {
            string fileName = DateTime.UtcNow.ToString("yyyy-MM-dd") + ".json";
            currentLogFile = fileName;

            string path = Path.Combine(logDirectory, currentLogFile);
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
            JsonSerializerOptions options = new JsonSerializerOptions
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

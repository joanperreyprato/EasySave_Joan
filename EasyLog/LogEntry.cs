using System;
using System.IO;
using System.Text.Json.Serialization;

namespace EasyLog
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string BackupName { get; set; }
        public string SourceFilePath { get; set; }
        public string TargetFilePath { get; set; }
        public long FileSize { get; set; }
        public long TransferTime { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EntryType Type { get; set; }

        public LogEntry(string backupName, string source, string target, long size, long transferTime, EntryType type)
        {
            BackupName = backupName;
            SourceFilePath = source;
            TargetFilePath = target;
            FileSize = size;
            TransferTime = transferTime;
            Timestamp = DateTime.UtcNow;
            Type = type;
        }

        public enum EntryType
        {
            Summary,
            File
        }

        public string ToUNCPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path ?? string.Empty;

            try
            {
                string full = Path.GetFullPath(path);
                return full.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }
            catch
            {
                return path;
            }
        }
    }
}

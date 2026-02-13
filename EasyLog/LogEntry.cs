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
        // EncryptionTime semantics:
        //  0 : no encryption
        // >0 : encryption time in milliseconds
        // <0 : error code (negative)
        public long EncryptionTime { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EntryType Type { get; set; }

        public LogEntry(string backupName, string source, string target, long size, long transferTime, EntryType type, long encryptionTime = 0)
        {
            BackupName = backupName;
            SourceFilePath = source;
            TargetFilePath = target;
            FileSize = size;
            TransferTime = transferTime;
            EncryptionTime = encryptionTime;
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

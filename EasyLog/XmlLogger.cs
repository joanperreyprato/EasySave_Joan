using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace EasyLog
{
    public class XmlLogger : ILogger
    {
        private readonly string logDirectory;
        private string currentLogFile;
        public LogFormat Format => LogFormat.Xml;

        public XmlLogger(string logDirectory)
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
                string path = Path.Combine(logDirectory, currentLogFile);
                var doc = new XDocument(new XElement("log",
                    new XElement("timestamp", entry.Timestamp),
                    new XElement("backupName", entry.BackupName),
                    new XElement("sourceFilePath", entry.SourceFilePath),
                    new XElement("targetFilePath", entry.TargetFilePath),
                    new XElement("fileSize", entry.FileSize),
                    new XElement("transferTime", entry.TransferTime),
                    new XElement("type", entry.Type.ToString())
                ));

                File.AppendAllText(path, doc.ToString() + Environment.NewLine);
            }
            catch
            {
            }
        }

        public void CreateDailyLogFile()
        {
            string fileName = DateTime.UtcNow.ToString("yyyy-MM-dd") + ".xml";
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
    }
}

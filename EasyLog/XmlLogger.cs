using System.Text;
using System.Xml;

namespace EasyLog;

/// <summary>
/// Writes log entries to a daily XML file with a valid root element.
/// Thread-safe. Rotates automatically each day.
/// </summary>
public sealed class XmlLogger : ILogger
{
    private readonly string _logDirectory;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new <see cref="XmlLogger"/> writing to the specified directory.
    /// </summary>
    /// <param name="logDirectory">
    /// The directory where daily log files are created.
    /// Created automatically if it does not exist.
    /// </param>
    public XmlLogger(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
    }

    /// <summary>
    /// Appends a log entry to today's XML log file.
    /// Creates the file with a valid XML document if it does not yet exist.
    /// </summary>
    /// <param name="entry">The log entry to append.</param>
    public void Log(LogEntry entry)
    {
        entry.SourceFilePath = LogEntry.ToUncPath(entry.SourceFilePath);
        entry.TargetFilePath = LogEntry.ToUncPath(entry.TargetFilePath);

        string filePath = GetLogFilePath();
        string entryXml = BuildEntryXml(entry);

        lock (_lock)
        {
            if (!File.Exists(filePath))
            {
                // New file: full valid XML document
                string xml =
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                    "<Logs>\n" +
                    entryXml +
                    "</Logs>";
                File.WriteAllText(filePath, xml, Encoding.UTF8);
            }
            else
            {
                // Existing file: insert <Entry> before </Logs>
                string content = File.ReadAllText(filePath, Encoding.UTF8);
                int closingIndex = content.LastIndexOf("</Logs>", StringComparison.Ordinal);
                if (closingIndex >= 0)
                {
                    content = content.Insert(closingIndex, entryXml);
                    File.WriteAllText(filePath, content, Encoding.UTF8);
                }
            }
        }
    }

    /// <summary>Builds the XML string for a single log entry.</summary>
    private static string BuildEntryXml(LogEntry entry)
    {
        // Use XmlWriter for proper escaping of special characters in paths
        var sb = new StringBuilder();
        using (var writer = XmlWriter.Create(sb, new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = true,
            ConformanceLevel = ConformanceLevel.Fragment
        }))
        {
            writer.WriteStartElement("Entry");
            writer.WriteElementString("Timestamp", entry.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
            writer.WriteElementString("BackupName", entry.BackupName);
            writer.WriteElementString("SourceFilePath", entry.SourceFilePath);
            writer.WriteElementString("TargetFilePath", entry.TargetFilePath);
            writer.WriteElementString("FileSize", entry.FileSize.ToString());
            writer.WriteElementString("TransferDurationMs", entry.TransferDurationMs.ToString());
            writer.WriteElementString("EncryptionDurationMs", entry.EncryptionDurationMs.ToString());
            writer.WriteEndElement();
        }
        return sb.ToString() + "\n";
    }

    /// <summary>Returns the full path of today's log file (YYYY-MM-DD.xml).</summary>
    private string GetLogFilePath()
        => Path.Combine(_logDirectory, $"{DateTime.Now:yyyy-MM-dd}.xml");
}

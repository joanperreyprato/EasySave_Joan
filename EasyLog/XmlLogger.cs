using System.Text;
using System.Xml;

namespace EasyLog;

/// <summary>
/// Writes log entries to a daily XML file (YYYY-MM-DD.xml) with a single &lt;Logs&gt; root element.
/// </summary>
public sealed class XmlLogger : FileLogger
{
    private static readonly XmlWriterSettings WriterSettings = new()
    {
        Indent = true,
        IndentChars = "  ",
        OmitXmlDeclaration = true,
        ConformanceLevel = ConformanceLevel.Fragment
    };

    /// <summary>
    /// Initializes a new <see cref="XmlLogger"/> writing to the specified directory.
    /// </summary>
    /// <param name="logDirectory">
    /// The directory where daily log files are created.
    /// Created automatically if it does not exist.
    /// </param>
    public XmlLogger(string logDirectory) : base(logDirectory) { }

    /// <inheritdoc/>
    protected override string Extension => "xml";

    /// <inheritdoc/>
    protected override string Header => "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Logs>";

    /// <inheritdoc/>
    protected override string Footer => "\n</Logs>";

    /// <inheritdoc/>
    protected override string Separator => "\n";

    /// <inheritdoc/>
    protected override string Serialize(LogEntry entry)
    {
        // XmlWriter escapes special characters found in paths and names
        var sb = new StringBuilder();
        using (var writer = XmlWriter.Create(sb, WriterSettings))
        {
            writer.WriteStartElement("Entry");
            writer.WriteElementString("Timestamp", entry.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
            writer.WriteElementString("BackupName", entry.BackupName);
            writer.WriteElementString("Action", entry.Action);
            writer.WriteElementString("MachineName", entry.MachineName);
            writer.WriteElementString("UserName", entry.UserName);
            writer.WriteElementString("SourceFilePath", entry.SourceFilePath);
            writer.WriteElementString("TargetFilePath", entry.TargetFilePath);
            writer.WriteElementString("FileSize", entry.FileSize.ToString());
            writer.WriteElementString("TransferDurationMs", entry.TransferDurationMs.ToString());
            writer.WriteElementString("EncryptionDurationMs", entry.EncryptionDurationMs.ToString());
            writer.WriteEndElement();
        }
        return sb.ToString();
    }
}

using System.Text;

namespace EasyLog;

/// <summary>
/// Base class for daily file loggers (one file per day, e.g. 2026-10-21.json).
/// Each file is a single well-formed document (JSON array or XML root element):
/// new entries are inserted just before the closing token without rewriting the file.
/// Thread-safe across every logger instance of the process.
/// </summary>
public abstract class FileLogger : ILogger
{
    private const int TailSize = 4096;
    private static readonly object FileLock = new();
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly string _logDirectory;

    /// <summary>Initializes the logger and creates the log directory if needed.</summary>
    /// <param name="logDirectory">The directory where daily log files are created.</param>
    protected FileLogger(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
    }

    /// <summary>Gets the file extension without the dot (e.g. "json").</summary>
    protected abstract string Extension { get; }

    /// <summary>Gets the text that opens a new file (e.g. "[").</summary>
    protected abstract string Header { get; }

    /// <summary>Gets the text that closes the file (e.g. "\n]").</summary>
    protected abstract string Footer { get; }

    /// <summary>Gets the text written between two entries (e.g. ",\n").</summary>
    protected abstract string Separator { get; }

    /// <summary>Serializes one entry as a fragment of the document.</summary>
    protected abstract string Serialize(LogEntry entry);

    /// <inheritdoc/>
    public void Log(LogEntry entry)
    {
        entry.SourceFilePath = LogEntry.ToUncPath(entry.SourceFilePath);
        entry.TargetFilePath = LogEntry.ToUncPath(entry.TargetFilePath);

        string body = Serialize(entry);
        string filePath = Path.Combine(_logDirectory, $"{entry.Timestamp:yyyy-MM-dd}.{Extension}");

        lock (FileLock)
            Append(filePath, body);
    }

    private void Append(string filePath, string body)
    {
        using var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        if (stream.Length == 0)
        {
            Write(stream, Header + "\n" + body + Footer);
            return;
        }

        string tail = ReadTail(stream);
        int closingIndex = tail.LastIndexOf(Footer.Trim(), StringComparison.Ordinal);
        if (closingIndex < 0)
        {
            // Malformed file (e.g. edited by hand): append without losing anything
            stream.Seek(0, SeekOrigin.End);
            Write(stream, Separator + body);
            return;
        }

        string beforeClosing = tail[..closingIndex].TrimEnd();
        bool hasEntries = !beforeClosing.EndsWith(Header, StringComparison.Ordinal);
        long cutPosition = stream.Length - Utf8.GetByteCount(tail[beforeClosing.Length..]);

        stream.SetLength(cutPosition);
        stream.Seek(cutPosition, SeekOrigin.Begin);
        Write(stream, (hasEntries ? Separator : "\n") + body + Footer);
    }

    private static string ReadTail(FileStream stream)
    {
        int length = (int)Math.Min(TailSize, stream.Length);
        var buffer = new byte[length];
        stream.Seek(-length, SeekOrigin.End);
        stream.ReadExactly(buffer);
        return Utf8.GetString(buffer);
    }

    private static void Write(FileStream stream, string text)
    {
        byte[] bytes = Utf8.GetBytes(text);
        stream.Write(bytes);
    }
}

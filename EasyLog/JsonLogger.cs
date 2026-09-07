using System.Text;
using System.Text.Json;

namespace EasyLog;

/// <summary>
/// Writes log entries to a daily JSON file.
/// The file contains a valid JSON array, human-readable via Notepad.
/// Thread-safe. Rotates automatically each day.
/// </summary>
public sealed class JsonLogger : ILogger
{
    private readonly string _logDirectory;
    private readonly object _lock = new();

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes a new <see cref="JsonLogger"/> writing to the specified directory.
    /// </summary>
    /// <param name="logDirectory">
    /// The directory where daily log files are created.
    /// The directory is created automatically if it does not exist.
    /// </param>
    public JsonLogger(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
    }

    /// <summary>
    /// Appends a log entry to today's JSON log file.
    /// Creates the file with a valid JSON array if it does not yet exist.
    /// </summary>
    /// <param name="entry">The log entry to append.</param>
    public void Log(LogEntry entry)
    {
        // Normalize to UNC
        entry.SourceFilePath = LogEntry.ToUncPath(entry.SourceFilePath);
        entry.TargetFilePath = LogEntry.ToUncPath(entry.TargetFilePath);

        string filePath = GetLogFilePath();
        string serialized = JsonSerializer.Serialize(entry, SerializerOptions);

        lock (_lock)
        {
            if (!File.Exists(filePath))
            {
                // New file: create a valid JSON array with one element
                File.WriteAllText(filePath, $"[\n{serialized}\n]", Encoding.UTF8);
            }
            else
            {
                // Existing file: insert new entry before closing bracket
                string content = File.ReadAllText(filePath, Encoding.UTF8).TrimEnd();
                if (content.EndsWith(']'))
                    content = content[..^1].TrimEnd().TrimEnd(',');

                File.WriteAllText(filePath,
                    $"{content},\n{serialized}\n]",
                    Encoding.UTF8);
            }
        }
    }

    /// <summary>Returns the full path of today's log file (YYYY-MM-DD.json).</summary>
    private string GetLogFilePath()
        => Path.Combine(_logDirectory, $"{DateTime.Now:yyyy-MM-dd}.json");
}

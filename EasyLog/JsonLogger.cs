using System.Text.Json;

namespace EasyLog;

/// <summary>
/// Writes log entries to a daily JSON file (YYYY-MM-DD.json).
/// The file contains a valid, indented JSON array, human-readable via Notepad.
/// </summary>
public sealed class JsonLogger : FileLogger
{
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
    public JsonLogger(string logDirectory) : base(logDirectory) { }

    /// <inheritdoc/>
    protected override string Extension => "json";

    /// <inheritdoc/>
    protected override string Header => "[";

    /// <inheritdoc/>
    protected override string Footer => "\n]";

    /// <inheritdoc/>
    protected override string Separator => ",\n";

    /// <inheritdoc/>
    protected override string Serialize(LogEntry entry)
        => JsonSerializer.Serialize(entry, SerializerOptions);
}

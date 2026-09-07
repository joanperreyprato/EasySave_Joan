using System.Text.Json.Serialization;

namespace EasyLog;

/// <summary>
/// Represents a single entry in the daily backup log file.
/// All file paths are stored in UNC format for network compatibility.
/// </summary>
public sealed class LogEntry
{
    /// <summary>Gets or sets the timestamp of the log entry.</summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>Gets or sets the name of the backup job.</summary>
    [JsonPropertyName("backupName")]
    public string BackupName { get; set; } = string.Empty;

    /// <summary>Gets or sets the full UNC path of the source file.</summary>
    [JsonPropertyName("sourceFilePath")]
    public string SourceFilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the full UNC path of the destination file.</summary>
    [JsonPropertyName("targetFilePath")]
    public string TargetFilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the file size in bytes.</summary>
    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the file transfer duration in milliseconds.
    /// A negative value indicates a transfer error.
    /// </summary>
    [JsonPropertyName("transferDurationMs")]
    public long TransferDurationMs { get; set; }

    /// <summary>
    /// Gets or sets the file encryption duration in milliseconds.
    /// 0 = no encryption, positive = duration in ms, negative = error code.
    /// </summary>
    [JsonPropertyName("encryptionDurationMs")]
    public long EncryptionDurationMs { get; set; }

    /// <summary>
    /// Converts a local file path to UNC format (e.g. C:\foo → \\MachineName\C$\foo).
    /// If the path is already UNC or cannot be converted, returns it unchanged.
    /// </summary>
    /// <param name="path">The local or network file path to convert.</param>
    /// <returns>The path in UNC format.</returns>
    public static string ToUncPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        // Already a UNC path
        if (path.StartsWith(@"\\"))
            return path;

        // Local drive path: X:\... → \\MachineName\X$\...
        if (path.Length >= 2 && path[1] == ':')
        {
            string driveLetter = path[0].ToString().ToUpper();
            string rest = path.Length > 3 ? path[3..] : string.Empty;
            return $@"\\{Environment.MachineName}\{driveLetter}$\{rest}";
        }

        return path;
    }
}

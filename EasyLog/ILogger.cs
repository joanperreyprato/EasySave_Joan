namespace EasyLog;

/// <summary>
/// Defines the contract for a log writer used by EasySave backup jobs.
/// All implementations must be thread-safe.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Writes a log entry to the underlying storage.
    /// </summary>
    /// <param name="entry">The log entry to record.</param>
    void Log(LogEntry entry);
}

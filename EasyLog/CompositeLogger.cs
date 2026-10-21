namespace EasyLog;

/// <summary>
/// Composes multiple <see cref="ILogger"/> instances into a single logger.
/// Each entry is forwarded to all inner loggers independently.
/// If one logger throws, the others are still called.
/// Thread-safe.
/// </summary>
public sealed class CompositeLogger : ILogger, IDisposable
{
    private readonly IReadOnlyList<ILogger> _loggers;

    /// <summary>
    /// Initializes a new <see cref="CompositeLogger"/> with the given loggers.
    /// </summary>
    /// <param name="loggers">The loggers to delegate to.</param>
    public CompositeLogger(IEnumerable<ILogger> loggers)
    {
        _loggers = loggers?.ToList() ?? throw new ArgumentNullException(nameof(loggers));
    }

    /// <summary>
    /// Writes the log entry to every composed logger.
    /// Exceptions from individual loggers are silently caught to avoid blocking the backup.
    /// </summary>
    /// <param name="entry">The log entry to forward.</param>
    public void Log(LogEntry entry)
    {
        foreach (var logger in _loggers)
        {
            try { logger.Log(entry); }
            catch { /* Never block the backup due to a logging failure */ }
        }
    }

    /// <summary>Disposes every inner logger that holds resources (e.g. a TCP connection).</summary>
    public void Dispose()
    {
        foreach (var logger in _loggers.OfType<IDisposable>())
            logger.Dispose();
    }
}

namespace EasyLog;

/// <summary>
/// Factory for creating <see cref="ILogger"/> instances.
/// Provides a clean creation API without relying on reflection or service locators.
/// </summary>
public static class LoggerFactory
{
    /// <summary>
    /// Creates a single logger for the specified format and directory.
    /// </summary>
    /// <param name="format">The log file format (JSON or XML).</param>
    /// <param name="logDirectory">The directory where log files are written.</param>
    /// <returns>A new <see cref="ILogger"/> for the requested format.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for unknown formats.</exception>
    public static ILogger Create(LogFormat format, string logDirectory)
        => format switch
        {
            LogFormat.Json => new JsonLogger(logDirectory),
            LogFormat.Xml  => new XmlLogger(logDirectory),
            _              => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };

    /// <summary>
    /// Creates a <see cref="CompositeLogger"/> that delegates to all provided loggers.
    /// </summary>
    /// <param name="loggers">The loggers to compose.</param>
    /// <returns>A composite logger.</returns>
    public static ILogger CreateComposite(IEnumerable<ILogger> loggers)
        => new CompositeLogger(loggers);
}

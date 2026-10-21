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
    /// Creates the logger matching the chosen log mode (local file, centralized server, or both).
    /// </summary>
    /// <param name="mode">Where the entries must be written.</param>
    /// <param name="format">The local log file format (ignored in <see cref="LogMode.Centralized"/> mode).</param>
    /// <param name="logDirectory">The local log directory.</param>
    /// <param name="serverAddress">Host name or IP address of the centralized log server.</param>
    /// <param name="serverPort">TCP port of the centralized log server.</param>
    /// <returns>A logger; dispose it when the backup run is over.</returns>
    public static ILogger Create(LogMode mode, LogFormat format, string logDirectory,
        string serverAddress, int serverPort)
        => mode switch
        {
            LogMode.Local       => Create(format, logDirectory),
            LogMode.Centralized => new SocketLogger(serverAddress, serverPort),
            LogMode.Both        => CreateComposite([Create(format, logDirectory), new SocketLogger(serverAddress, serverPort)]),
            _                   => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

    /// <summary>
    /// Creates a <see cref="CompositeLogger"/> that delegates to all provided loggers.
    /// </summary>
    /// <param name="loggers">The loggers to compose.</param>
    /// <returns>A composite logger.</returns>
    public static ILogger CreateComposite(IEnumerable<ILogger> loggers)
        => new CompositeLogger(loggers);
}

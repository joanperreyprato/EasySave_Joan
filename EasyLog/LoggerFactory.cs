using System;
using System.Collections.Generic;

namespace EasyLog
{
    /// <summary>
    /// Factory class for creating loggers based on configuration
    /// </summary>
    public static class LoggerFactory
    {
        /// <summary>
        /// Creates a logger based on the provided configuration
        /// </summary>
        /// <param name="logDirectory">Local log directory</param>
        /// <param name="enableRemoteLogging">Whether to enable remote logging</param>
        /// <param name="remoteHost">Remote log server host</param>
        /// <param name="remotePort">Remote log server port</param>
        /// <param name="enableLocalLogging">Whether to keep local logging when remote is enabled</param>
        /// <param name="logFormat">Log format (Json or Xml)</param>
        /// <returns>An ILogger instance</returns>
        public static ILogger CreateLogger(
            string logDirectory,
            bool enableRemoteLogging = false,
            string remoteHost = "localhost",
            int remotePort = 5555,
            bool enableLocalLogging = true,
            string logFormat = "Json")
        {
            var loggers = new List<ILogger>();

            // Add local logger if enabled
            if (!enableRemoteLogging || enableLocalLogging)
            {
                ILogger localLogger = logFormat.ToLower() switch
                {
                    "xml" => new XmlLogger(logDirectory),
                    _ => new JsonLogger(logDirectory)
                };
                loggers.Add(localLogger);
            }

            // Add remote logger if enabled
            if (enableRemoteLogging)
            {
                try
                {
                    var socketLogger = new SocketLogger(remoteHost, remotePort);
                    loggers.Add(socketLogger);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to initialize remote logging: {ex.Message}");
                    // Continue with local logging only
                }
            }

            // Return appropriate logger
            if (loggers.Count == 0)
            {
                // Fallback to JSON logger if nothing else worked
                return new JsonLogger(logDirectory);
            }
            else if (loggers.Count == 1)
            {
                return loggers[0];
            }
            else
            {
                return new CompositeLogger(loggers.ToArray());
            }
        }

        /// <summary>
        /// Creates a logger from AppConfig
        /// </summary>
        public static ILogger CreateFromConfig(string logDirectory)
        {
            try
            {
                // Try to load config from EasySave.Core
                var configType = Type.GetType("EasySave.Core.AppConfig, EasySave.Core");
                if (configType != null)
                {
                    var instanceProperty = configType.GetProperty("Instance");
                    if (instanceProperty != null)
                    {
                        var config = instanceProperty.GetValue(null);
                        if (config != null)
                        {
                            var remoteLoggingProp = configType.GetProperty("RemoteLogging");
                            var loggingProp = configType.GetProperty("Logging");

                            if (remoteLoggingProp != null && loggingProp != null)
                            {
                                var remoteLogging = remoteLoggingProp.GetValue(config);
                                var logging = loggingProp.GetValue(config);

                                if (remoteLogging != null && logging != null)
                                {
                                    var remoteLoggingType = remoteLogging.GetType();
                                    var loggingType = logging.GetType();

                                    var enabled = (bool)(remoteLoggingType.GetProperty("Enabled")?.GetValue(remoteLogging) ?? false);
                                    var serverHost = (string)(remoteLoggingType.GetProperty("ServerHost")?.GetValue(remoteLogging) ?? "localhost");
                                    var serverPort = (int)(remoteLoggingType.GetProperty("ServerPort")?.GetValue(remoteLogging) ?? 5555);
                                    var enableLocalLogging = (bool)(remoteLoggingType.GetProperty("EnableLocalLogging")?.GetValue(remoteLogging) ?? true);
                                    var logFormat = (string)(loggingType.GetProperty("LogFormat")?.GetValue(logging) ?? "Json");

                                    return CreateLogger(
                                        logDirectory,
                                        enabled,
                                        serverHost,
                                        serverPort,
                                        enableLocalLogging,
                                        logFormat
                                    );
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to load logging config: {ex.Message}");
            }

            // Fallback to simple JSON logger
            return new JsonLogger(logDirectory);
        }
    }
}

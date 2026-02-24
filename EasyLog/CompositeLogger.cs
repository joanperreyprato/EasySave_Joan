using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyLog
{
    /// <summary>
    /// Logger that forwards log entries to multiple loggers
    /// Useful for logging both locally and remotely simultaneously
    /// </summary>
    public class CompositeLogger : ILogger
    {
        private readonly List<ILogger> _loggers = new();
        private readonly object _lockObject = new();

        public LogFormat Format { get; private set; }

        public CompositeLogger(params ILogger[] loggers)
        {
            if (loggers == null || loggers.Length == 0)
            {
                throw new ArgumentException("At least one logger must be provided", nameof(loggers));
            }

            _loggers.AddRange(loggers);
            // Use the format of the first logger
            Format = _loggers[0].Format;
        }

        public void AddLogger(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            lock (_lockObject)
            {
                if (!_loggers.Contains(logger))
                {
                    _loggers.Add(logger);
                }
            }
        }

        public void RemoveLogger(ILogger logger)
        {
            if (logger == null) return;

            lock (_lockObject)
            {
                _loggers.Remove(logger);
            }
        }

        public void LogAction(LogEntry entry)
        {
            if (entry == null) return;

            List<ILogger> loggersCopy;
            lock (_lockObject)
            {
                loggersCopy = new List<ILogger>(_loggers);
            }

            // Log to all loggers
            foreach (var logger in loggersCopy)
            {
                try
                {
                    logger.LogAction(entry);
                }
                catch (Exception)
                {
                    // If one logger fails, continue with others
                    // We don't want a remote logging failure to stop local logging
                }
            }
        }

        public void CreateDailyLogFile()
        {
            List<ILogger> loggersCopy;
            lock (_lockObject)
            {
                loggersCopy = new List<ILogger>(_loggers);
            }

            foreach (var logger in loggersCopy)
            {
                try
                {
                    logger.CreateDailyLogFile();
                }
                catch (Exception)
                {
                    // Continue with other loggers even if one fails
                }
            }
        }

        public int GetLoggerCount()
        {
            lock (_lockObject)
            {
                return _loggers.Count;
            }
        }

        public IReadOnlyList<ILogger> GetLoggers()
        {
            lock (_lockObject)
            {
                return _loggers.AsReadOnly();
            }
        }
    }
}

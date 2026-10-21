using System.Diagnostics;

namespace EasySave.Core.Services;

/// <summary>
/// Monitors whether a configured business software process is currently running.
/// Used to block or pause backup operations when the business application is active.
/// The answer is cached briefly because it is queried before every file transfer.
/// Thread-safe.
/// </summary>
public sealed class BusinessSoftwareMonitor
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMilliseconds(250);

    private readonly object _lock = new();
    private string _processName = string.Empty;
    private bool _lastResult;
    private DateTime _lastCheck = DateTime.MinValue;

    /// <summary>
    /// Gets or sets the name of the business software process to monitor (with or without .exe extension).
    /// </summary>
    public string ProcessName
    {
        get { lock (_lock) return _processName; }
        set
        {
            lock (_lock)
            {
                _processName = value ?? string.Empty;
                _lastCheck = DateTime.MinValue; // Invalidate the cache
            }
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the monitored business software is currently running.
    /// Returns <c>false</c> if no process name is configured.
    /// </summary>
    public bool IsRunning()
    {
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(_processName))
                return false;

            if (DateTime.UtcNow - _lastCheck < CacheDuration)
                return _lastResult;

            // Strip .exe suffix if present to match GetProcessesByName behavior
            string cleanName = _processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? _processName[..^4]
                : _processName;

            _lastResult = Process.GetProcessesByName(cleanName).Length > 0;
            _lastCheck = DateTime.UtcNow;
            return _lastResult;
        }
    }
}

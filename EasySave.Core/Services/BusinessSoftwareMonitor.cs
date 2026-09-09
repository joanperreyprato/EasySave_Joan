using System.Diagnostics;

namespace EasySave.Core.Services;

/// <summary>
/// Monitors whether a configured business software process is currently running.
/// Used to block or pause backup operations when the business application is active.
/// Thread-safe.
/// </summary>
public sealed class BusinessSoftwareMonitor
{
    private readonly object _lock = new();
    private string _processName = string.Empty;

    /// <summary>
    /// Gets or sets the name of the business software process to monitor (without .exe extension).
    /// </summary>
    public string ProcessName
    {
        get { lock (_lock) return _processName; }
        set { lock (_lock) _processName = value ?? string.Empty; }
    }

    /// <summary>
    /// Returns <c>true</c> if the monitored business software is currently running.
    /// Returns <c>false</c> if no process name is configured.
    /// </summary>
    public bool IsRunning()
    {
        string name;
        lock (_lock) name = _processName;

        if (string.IsNullOrWhiteSpace(name))
            return false;

        // Strip .exe suffix if present to match GetProcessesByName behavior
        string cleanName = name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? name[..^4]
            : name;

        return Process.GetProcessesByName(cleanName).Length > 0;
    }
}

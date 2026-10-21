namespace EasyLog;

/// <summary>Where daily log entries are written (v3.0 log centralization).</summary>
public enum LogMode
{
    /// <summary>Only in the daily file on the user's machine.</summary>
    Local,
    /// <summary>Only on the centralized Docker log server.</summary>
    Centralized,
    /// <summary>On the user's machine and on the centralized Docker log server.</summary>
    Both
}

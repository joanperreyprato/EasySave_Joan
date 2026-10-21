using System.Text.Json;
using System.Text.Json.Serialization;
using EasyLog;

namespace EasySave.Core.Services;

/// <summary>
/// Persists global application settings (business software name, encryption extensions, log format, etc.)
/// to %ProgramData%\EasySave\appsettings.json.
/// Thread-safe.
/// </summary>
public sealed class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly object _lock = new();
    private readonly string _settingsPath;
    private readonly SettingsData _data;

    // ── Backing data class ─────────────────────────────────────────────────────

    private sealed class SettingsData
    {
        public string BusinessSoftwareName { get; set; } = string.Empty;
        public List<string> EncryptionExtensions { get; set; } = [];
        public string LogFormat { get; set; } = "Json";
        public string Language { get; set; } = "en";
        public long LargeFileSizeKb { get; set; } = 1024;
        public List<string> PriorityExtensions { get; set; } = [];
        public LogMode LogMode { get; set; } = LogMode.Local;
        public string LogServerAddress { get; set; } = "127.0.0.1";
        public int LogServerPort { get; set; } = 8080;
    }

    /// <summary>Initializes the settings service, loading persisted values from disk.</summary>
    /// <param name="settingsPath">Optional custom path for testing; defaults to the ProgramData location.</param>
    public AppSettings(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? AppPaths.SettingsFile;
        _data = Load();
    }

    // ── Properties ─────────────────────────────────────────────────────────────

    /// <summary>Gets or sets the name of the business software process to monitor.</summary>
    public string BusinessSoftwareName
    {
        get { lock (_lock) return _data.BusinessSoftwareName; }
        set => Update(d => d.BusinessSoftwareName = value);
    }

    /// <summary>
    /// Gets or sets the file extensions (e.g. ".docx", ".xlsx") that should be encrypted by CryptoSoft.
    /// </summary>
    public List<string> EncryptionExtensions
    {
        get { lock (_lock) return [.. _data.EncryptionExtensions]; }
        set => Update(d => d.EncryptionExtensions = [.. value]);
    }

    /// <summary>Gets or sets the log format ("Json" or "Xml").</summary>
    public string LogFormat
    {
        get { lock (_lock) return _data.LogFormat; }
        set => Update(d => d.LogFormat = value);
    }

    /// <summary>Gets the log format as an <see cref="EasyLog.LogFormat"/> value (JSON when unknown).</summary>
    public EasyLog.LogFormat LogFileFormat
        => Enum.TryParse(LogFormat, ignoreCase: true, out EasyLog.LogFormat format) ? format : EasyLog.LogFormat.Json;

    /// <summary>Gets or sets the UI language code ("en" or "fr").</summary>
    public string Language
    {
        get { lock (_lock) return _data.Language; }
        set => Update(d => d.Language = value);
    }

    /// <summary>Gets or sets the large file threshold in kilobytes (v3.0: one large file transferred at a time).</summary>
    public long LargeFileSizeKb
    {
        get { lock (_lock) return _data.LargeFileSizeKb; }
        set => Update(d => d.LargeFileSizeKb = value);
    }

    /// <summary>Alias for <see cref="LargeFileSizeKb"/> — max file size (KB) for parallel transfer.</summary>
    public long MaxParallelFileSizeKb
    {
        get => LargeFileSizeKb;
        set => LargeFileSizeKb = value;
    }

    /// <summary>Gets or sets priority file extensions (v3.0 priority rule).</summary>
    public List<string> PriorityExtensions
    {
        get { lock (_lock) return [.. _data.PriorityExtensions]; }
        set => Update(d => d.PriorityExtensions = [.. value]);
    }

    /// <summary>Gets or sets where daily logs are written (local, centralized server, or both).</summary>
    public LogMode LogMode
    {
        get { lock (_lock) return _data.LogMode; }
        set => Update(d => d.LogMode = value);
    }

    /// <summary>Gets or sets the host name or IP address of the centralized log server.</summary>
    public string LogServerAddress
    {
        get { lock (_lock) return _data.LogServerAddress; }
        set => Update(d => d.LogServerAddress = value);
    }

    /// <summary>Gets or sets the TCP port of the centralized log server.</summary>
    public int LogServerPort
    {
        get { lock (_lock) return _data.LogServerPort; }
        set => Update(d => d.LogServerPort = value);
    }

    /// <summary>
    /// Creates the logger matching the current settings (format, mode and server).
    /// Dispose it when the backup run is over.
    /// </summary>
    /// <param name="logDirectory">Optional local log directory; defaults to <see cref="AppPaths.LogDirectory"/>.</param>
    public ILogger CreateLogger(string? logDirectory = null)
        => LoggerFactory.Create(LogMode, LogFileFormat, logDirectory ?? AppPaths.LogDirectory,
            LogServerAddress, LogServerPort);

    // ── Persistence ────────────────────────────────────────────────────────────

    private void Update(Action<SettingsData> change)
    {
        lock (_lock)
        {
            change(_data);
            Save();
        }
    }

    private SettingsData Load()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return new SettingsData();
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<SettingsData>(json, JsonOptions) ?? new SettingsData();
        }
        catch { return new SettingsData(); }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(_data, JsonOptions));
        }
        catch { /* Ignore write errors */ }
    }
}

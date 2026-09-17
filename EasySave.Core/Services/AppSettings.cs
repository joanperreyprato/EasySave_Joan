using System.Text.Json;

namespace EasySave.Core.Services;

/// <summary>
/// Persists global application settings (business software name, encryption extensions, log format, etc.)
/// to %ProgramData%\EasySave\appsettings.json.
/// Thread-safe singleton-style service.
/// </summary>
public sealed class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "EasySave", "appsettings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly object _lock = new();

    // ── Backing data class ─────────────────────────────────────────────────────

    private sealed class SettingsData
    {
        public string BusinessSoftwareName { get; set; } = string.Empty;
        public List<string> EncryptionExtensions { get; set; } = [];
        public string LogFormat { get; set; } = "Json";
        public string Language { get; set; } = "en";
        public long LargeFileSizeKb { get; set; } = 1024;
        public List<string> PriorityExtensions { get; set; } = [];
    }

    private SettingsData _data;

    /// <summary>Initializes the settings service, loading persisted values from disk.</summary>
    /// <param name="settingsPath">Optional custom path for testing; defaults to ProgramData location.</param>
    public AppSettings(string? settingsPath = null)
    {
        if (settingsPath is not null)
            _settingsPath = settingsPath;
        _data = Load();
    }

    // Override the static field with an instance field for testability
    private readonly string _settingsPath;

    // ── Properties ─────────────────────────────────────────────────────────────

    /// <summary>Gets or sets the name of the business software process to monitor.</summary>
    public string BusinessSoftwareName
    {
        get { lock (_lock) return _data.BusinessSoftwareName; }
        set { lock (_lock) { _data.BusinessSoftwareName = value; Save(); } }
    }

    /// <summary>
    /// Gets or sets the file extensions (e.g. ".docx", ".xlsx") that should be encrypted by CryptoSoft.
    /// </summary>
    public List<string> EncryptionExtensions
    {
        get { lock (_lock) return [.. _data.EncryptionExtensions]; }
        set { lock (_lock) { _data.EncryptionExtensions = value; Save(); } }
    }

    /// <summary>Gets or sets the log format ("Json" or "Xml").</summary>
    public string LogFormat
    {
        get { lock (_lock) return _data.LogFormat; }
        set { lock (_lock) { _data.LogFormat = value; Save(); } }
    }

    /// <summary>Gets or sets the UI language code ("en" or "fr").</summary>
    public string Language
    {
        get { lock (_lock) return _data.Language; }
        set { lock (_lock) { _data.Language = value; Save(); } }
    }

    /// <summary>Gets or sets the large file threshold in kilobytes (for v3.0 parallel transfers).</summary>
    public long LargeFileSizeKb
    {
        get { lock (_lock) return _data.LargeFileSizeKb; }
        set { lock (_lock) { _data.LargeFileSizeKb = value; Save(); } }
    }

    /// <summary>Gets or sets priority file extensions (for v3.0 priority queue).</summary>
    public List<string> PriorityExtensions
    {
        get { lock (_lock) return [.. _data.PriorityExtensions]; }
        set { lock (_lock) { _data.PriorityExtensions = value; Save(); } }
    }

    /// <summary>Alias for LargeFileSizeKb — max file size (Ko) for parallel transfer.</summary>
    public long MaxParallelFileSizeKb
    {
        get => LargeFileSizeKb;
        set => LargeFileSizeKb = value;
    }

    // ── Persistence ────────────────────────────────────────────────────────────

    private SettingsData Load()
    {
        try
        {
            string targetPath = _settingsPath ?? SettingsPath;
            if (!File.Exists(targetPath)) return new SettingsData();
            var json = File.ReadAllText(targetPath);
            return JsonSerializer.Deserialize<SettingsData>(json, JsonOptions) ?? new SettingsData();
        }
        catch { return new SettingsData(); }
    }

    private void Save()
    {
        try
        {
            string targetPath = _settingsPath ?? SettingsPath;
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            File.WriteAllText(targetPath, JsonSerializer.Serialize(_data, JsonOptions));
        }
        catch { /* Ignore write errors */ }
    }
}

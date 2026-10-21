namespace EasySave.Core.Services;

/// <summary>
/// Default locations of every EasySave data file.
/// Everything lives under %ProgramData%\EasySave so that it works for every user of a server
/// (no hard-coded paths such as C:\temp).
/// </summary>
public static class AppPaths
{
    /// <summary>Gets the root data directory (%ProgramData%\EasySave).</summary>
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EasySave");

    /// <summary>Gets the directory of the daily log files.</summary>
    public static string LogDirectory => Path.Combine(DataDirectory, "Logs");

    /// <summary>Gets the real-time state file.</summary>
    public static string StateFile => Path.Combine(DataDirectory, "state.json");

    /// <summary>Gets the general settings file.</summary>
    public static string SettingsFile => Path.Combine(DataDirectory, "appsettings.json");

    /// <summary>Gets the file storing the user interface language.</summary>
    public static string LanguageFile => Path.Combine(DataDirectory, "language.json");

    /// <summary>Gets the path of a backup job configuration file.</summary>
    /// <param name="fileName">The configuration file name (e.g. "backup_config.json").</param>
    public static string JobsFile(string fileName) => Path.Combine(DataDirectory, fileName);
}

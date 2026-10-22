using EasySave.Core.Services;

namespace EasySave.CLI;

/// <summary>
/// Entry point of EasySave.exe, the command-line interface of EasySave 3.0.
/// Without arguments it shows the interactive menu; with a job selection
/// ("EasySave.exe 1-3", "EasySave.exe 1;3") it runs those jobs directly, as in version 1.0.
/// </summary>
internal static class Program
{
    private const string LogDirOption = "--logdir=";
    private const string LogDirShortOption = "-l=";

    /// <summary>
    /// Application entry point. Parses arguments and delegates to the appropriate mode.
    /// </summary>
    /// <param name="args">Command-line arguments. Empty = interactive mode.</param>
    /// <returns>0 on success, 1 if a job did not complete or EasySave is already running.</returns>
    private static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8; // Accents and symbols (✓, ✗, ─)
        var languageProvider = LanguageProvider.Instance;

        // ── Mono-Instance: block if another EasySave (WPF or CLI) is running ──
        using var mutex = new Mutex(true, @"Global\EasySaveMutex_ProSoft", out bool isNewInstance);
        if (!isNewInstance)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ {languageProvider.Get("error_already_running")}");
            Console.ResetColor();
            return 1;
        }

        // Same services, jobs and settings as the graphical interface
        var stateManager = new StateManager();
        var settings = new AppSettings();
        languageProvider.SetLanguage(settings.Language); // Same language as the graphical interface
        var monitor = new BusinessSoftwareMonitor { ProcessName = settings.BusinessSoftwareName };
        using var backupService = new BackupServiceV3(stateManager, settings, monitor);

        var ui = new ConsoleMenu(backupService, stateManager, languageProvider, settings, ResolveLogDirectory(args));
        var jobArgs = args.Where(a => !IsLogDirOption(a)).ToArray();

        if (jobArgs.Length == 0)
        {
            ui.Run();
            return 0;
        }
        return ui.ExecuteFromArgs(jobArgs);
    }

    /// <summary>
    /// Returns the log directory given with --logdir=PATH or -l=PATH,
    /// or <c>null</c> to use the default location.
    /// </summary>
    /// <param name="args">Raw command-line arguments.</param>
    private static string? ResolveLogDirectory(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg.StartsWith(LogDirOption, StringComparison.OrdinalIgnoreCase))
                return arg[LogDirOption.Length..];
            if (arg.StartsWith(LogDirShortOption, StringComparison.OrdinalIgnoreCase))
                return arg[LogDirShortOption.Length..];
        }
        return null;
    }

    private static bool IsLogDirOption(string arg)
        => arg.StartsWith(LogDirOption, StringComparison.OrdinalIgnoreCase)
        || arg.StartsWith(LogDirShortOption, StringComparison.OrdinalIgnoreCase);
}

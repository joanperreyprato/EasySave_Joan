using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;
using EasyLog;

namespace EasySave.CLI;

/// <summary>
/// Entry point for the EasySave command-line interface application.
/// Supports both interactive menu mode and direct command-line execution.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Application entry point. Parses arguments and delegates to the appropriate mode.
    /// </summary>
    /// <param name="args">Command-line arguments. Empty = interactive mode.</param>
    private static void Main(string[] args)
    {
        // ── Mono-Instance: block if another EasySave (WPF or CLI) is running ──
        using var mutex = new System.Threading.Mutex(true, @"Global\EasySaveMutex_ProSoft", out bool isNewInstance);
        if (!isNewInstance)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ EasySave is already running / EasySave est déjà en cours d'exécution.");
            Console.ResetColor();
            return;
        }

        // Bootstrap services
        var stateManager = new StateManager();
        var languageProvider = LanguageProvider.Instance;
        var backupService = new BackupService(stateManager);

        // Determine log directory from args (--logdir=PATH or -l=PATH)
        string logDirectory = ResolveLogDirectory(args);
        ILogger logger = LoggerFactory.Create(LogFormat.Json, logDirectory);

        var ui = new ConsoleMenu(backupService, languageProvider, logger, logDirectory);

        if (args.Length == 0 || args.All(a => a.StartsWith("--logdir") || a.StartsWith("-l=")))
        {
            // Interactive mode
            ui.Run();
        }
        else
        {
            // Command-line mode: execute backup jobs directly
            ui.ExecuteFromArgs(args);
        }
    }

    /// <summary>
    /// Resolves the log directory from command-line arguments.
    /// Falls back to the default ProgramData location.
    /// </summary>
    /// <param name="args">Raw command-line arguments.</param>
    /// <returns>Absolute path to the log directory.</returns>
    private static string ResolveLogDirectory(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg.StartsWith("--logdir=", StringComparison.OrdinalIgnoreCase))
                return arg["--logdir=".Length..];
            if (arg.StartsWith("-l=", StringComparison.OrdinalIgnoreCase))
                return arg["-l=".Length..];
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "EasySave", "Logs");
    }
}

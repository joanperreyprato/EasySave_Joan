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
        // Bootstrap services
        var stateManager = new StateManager();
        var languageProvider = LanguageProvider.Instance;
        var backupService = new BackupService(stateManager);

        // Determine log directory and format from args (--logdir=PATH, --format=json|xml)
        string logDirectory = ResolveLogDirectory(args);
        LogFormat logFormat = ResolveLogFormat(args);
        ILogger logger = LoggerFactory.Create(logFormat, logDirectory);

        var ui = new ConsoleMenu(backupService, languageProvider, logger, logDirectory);

        if (args.Length == 0 || args.All(a => a.StartsWith("--logdir") || a.StartsWith("-l=") || a.StartsWith("--format") || a.StartsWith("-f=")))
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

    /// <summary>
    /// Resolves the log format from command-line arguments (--format=xml or -f=xml).
    /// Defaults to JSON.
    /// </summary>
    private static LogFormat ResolveLogFormat(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg.StartsWith("--format=", StringComparison.OrdinalIgnoreCase))
            {
                var val = arg["--format=".Length..];
                if (val.Equals("xml", StringComparison.OrdinalIgnoreCase))
                    return LogFormat.Xml;
            }
            if (arg.StartsWith("-f=", StringComparison.OrdinalIgnoreCase))
            {
                var val = arg["-f=".Length..];
                if (val.Equals("xml", StringComparison.OrdinalIgnoreCase))
                    return LogFormat.Xml;
            }
        }
        return LogFormat.Json;
    }
}

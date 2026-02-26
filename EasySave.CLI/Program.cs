using EasySave.Core;
using EasySave.CLI;
using System;
using System.IO;
using System.Linq;
using System.Threading;

// Prevent multiple instances using a named mutex (requirement from spec)
bool _createdNew;
var _singleInstanceMutex = new Mutex(true, "Global\\EasySave_SingleInstance", out _createdNew);
if (!_createdNew)
{
    Console.WriteLine("Another instance is already running. Exiting.");
    return;
}

// Initialize core services
LanguageProvider language = LanguageProvider.GetInstance("en");
StateManager stateManager = new();
stateManager.LoadState();
// Precompute tokens so CLI options can be read early (e.g. --logdir)
string[] tokens = args != null && args.Length > 0
    ? args.Select(a => a.Trim()).Where(a => !string.IsNullOrWhiteSpace(a)).ToArray()
    : Array.Empty<string>();

// Determine log directory: prefer explicit CLI option --logdir or --logdir=path, otherwise use CommonApplicationData\EasySave\logs
string defaultLogDir;
try
{
    string appData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
    defaultLogDir = Path.Combine(appData, "EasySave", "logs");
}
catch
{
    defaultLogDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
}

string logDir = defaultLogDir;
for (int i = 0; i < tokens.Length; i++)
{
    string t = tokens[i];
    if (t.StartsWith("--logdir=", StringComparison.OrdinalIgnoreCase))
    {
        logDir = t.Substring("--logdir=".Length);
        break;
    }
    if (string.Equals(t, "--logdir", StringComparison.OrdinalIgnoreCase) && i + 1 < tokens.Length)
    {
        logDir = tokens[i + 1];
        break;
    }
    if (t.StartsWith("-l=", StringComparison.OrdinalIgnoreCase))
    {
        logDir = t.Substring(3);
        break;
    }
}

try { logDir = Path.GetFullPath(string.IsNullOrWhiteSpace(logDir) ? defaultLogDir : logDir); } catch { logDir = defaultLogDir; }
Directory.CreateDirectory(logDir);

var logger = new EasyLog.JsonLogger(logDir);
BackupManager backupManager = new(stateManager, logger);
ConsoleUI ui = new(backupManager, language);

if (tokens != null && tokens.Length > 0)
{
    // Simple command-line support

    if (tokens.Any(t => string.Equals(t, "list", StringComparison.OrdinalIgnoreCase)))
    {
        ui.DisplayBackupJobs();
        return;
    }

    // If args are only indices (ex. "1-3" or "1;3"), execute those jobs directly
    int[] directIndices = CommandLineParser.ParseArguments(tokens);
    bool hasExplicitCommand = tokens.Any(t => string.Equals(t, "create", StringComparison.OrdinalIgnoreCase)
                                             || string.Equals(t, "list", StringComparison.OrdinalIgnoreCase)
                                             || string.Equals(t, "execute", StringComparison.OrdinalIgnoreCase));

    if (directIndices.Length > 0 && !hasExplicitCommand)
    {
        backupManager.ExecuteBackupJobs(directIndices);
        return;
    }

    if (tokens.Any(t => string.Equals(t, "create", StringComparison.OrdinalIgnoreCase)))
    {
        // Expect: create <name> <source> <target> [full|diff]
        int idx = Array.FindIndex(tokens, t => string.Equals(t, "create", StringComparison.OrdinalIgnoreCase));
        if (idx >= 0 && tokens.Length > idx + 3)
        {
            string name = tokens[idx + 1];
            string source = tokens[idx + 2];
            string target = tokens[idx + 3];
            BackupType type = BackupType.Full;
            if (tokens.Length > idx + 4 && tokens[idx + 4].StartsWith("d", StringComparison.OrdinalIgnoreCase))
                type = BackupType.Differential;

            if (backupManager.CreateBackupJob(name, source, target, type, out var error))
            {
                Console.WriteLine(language.GetText("backup.create.success"));
            }
            else
            {
                Console.WriteLine(language.GetText("backup.create.error.title"));
                Console.WriteLine(GetErrorMessage(error, language));
            }
        }
        else
        {
            Console.WriteLine("Usage: create <name> <source> <target> [full|diff]");
        }

        return;
    }

    if (tokens.Any(t => string.Equals(t, "execute", StringComparison.OrdinalIgnoreCase)))
    {
        // Parse indices from args
        int[] indices = CommandLineParser.ParseArguments(tokens);
        if (indices.Length == 0)
        {
            Console.WriteLine("No indices provided. Use execute <index> or execute 1-2 or execute 1,2");
            return;
        }

        backupManager.ExecuteBackupJobs(indices);
        return;
    }

    // Unknown or help
    Console.WriteLine("EasySave - commands: list | create | execute");
    return;
}

// Interactive mode
bool running = true;
while (running)
{
    ui.DisplayMainMenu();
    string? choice = Console.ReadLine();
    switch (choice)
    {
        case "1":
            ui.CreateBackupJobMenu();
            break;
        case "2":
            ui.ExecuteBackupMenu();
            break;
        case "3":
            ui.DisplayBackupJobs();
            break;
        case "4":
            ui.EditBackupJobMenu();
            break;
        case "5":
            ui.ChangeLanguageMenu();
            string? langChoice = Console.ReadLine();
            switch (langChoice)
            {
                case "1":
                    language.SetLanguage("en");
                    Console.WriteLine("Language changed to English.");
                    break;
                case "2":
                    language.SetLanguage("fr");
                    Console.WriteLine("Langue changée en Français.");
                    break;
                default:
                    Console.WriteLine(language.GetText("error.notfound"));
                    break;
            }
            break;
        case "6":
            running = false;
            break;
        default:
            Console.WriteLine(language.GetText("error.notfound"));
            break;
    }

    Console.WriteLine();
}

// Helper function to convert BackupCreationError to user-friendly message
string GetErrorMessage(BackupManager.BackupCreationError error, LanguageProvider languageProvider)
{
    return error switch
    {
        BackupManager.BackupCreationError.InvalidName => languageProvider.GetText("backup.create.error.invalid_name"),
        BackupManager.BackupCreationError.SourcePathInvalid => languageProvider.GetText("backup.create.error.source_path_invalid"),
        BackupManager.BackupCreationError.SourcePathNotFound => languageProvider.GetText("backup.create.error.source_path_not_found"),
        BackupManager.BackupCreationError.TargetPathInvalid => languageProvider.GetText("backup.create.error.target_path_invalid"),
        BackupManager.BackupCreationError.TargetPathNotFound => languageProvider.GetText("backup.create.error.target_path_not_found"),
        BackupManager.BackupCreationError.DuplicateName => languageProvider.GetText("backup.create.error.duplicate_name"),
        BackupManager.BackupCreationError.MaxJobsExceeded => languageProvider.GetText("backup.create.error.max_jobs_exceeded"),
        _ => languageProvider.GetText("backup.create.error.unknown")
    };
}

// Keep the mutex alive for the lifetime of the process to maintain the single-instance guarantee.
GC.KeepAlive(_singleInstanceMutex);

using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasyLog;

namespace EasySave.CLI;

/// <summary>
/// Provides the interactive console menu and command-line execution logic for EasySave.
/// </summary>
public sealed class ConsoleMenu
{
    private readonly IBackupService _backupService;
    private readonly ILanguageProvider _lang;
    private ILogger _logger;
    private string _logDirectory;

    /// <summary>
    /// Initializes a new instance of <see cref="ConsoleMenu"/>.
    /// </summary>
    /// <param name="backupService">The backup service to use.</param>
    /// <param name="languageProvider">The language provider for localization.</param>
    /// <param name="logger">The logger for backup operations.</param>
    /// <param name="logDirectory">The current log directory path.</param>
    public ConsoleMenu(IBackupService backupService, ILanguageProvider languageProvider,
        ILogger logger, string logDirectory)
    {
        _backupService = backupService;
        _lang = languageProvider;
        _logger = logger;
        _logDirectory = logDirectory;
    }

    /// <summary>
    /// Runs the main interactive menu loop until the user chooses to quit.
    /// </summary>
    public void Run()
    {
        while (true)
        {
            PrintHeader();
            Console.WriteLine($"  1. {_lang.Get("menu_list")}");
            Console.WriteLine($"  2. {_lang.Get("menu_create")}");
            Console.WriteLine($"  3. {_lang.Get("menu_execute")}");
            Console.WriteLine($"  4. {_lang.Get("menu_delete")}");
            Console.WriteLine($"  5. {_lang.Get("menu_settings")}");
            Console.WriteLine($"  0. {_lang.Get("menu_quit")}");
            Console.WriteLine();
            Console.Write(">>> ");

            var choice = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (choice)
            {
                case "1": ListJobs(); break;
                case "2": CreateJob(); break;
                case "3": ExecuteJobs(); break;
                case "4": DeleteJob(); break;
                case "5": ShowSettings(); break;
                case "0": return;
                default: PrintWarning("Invalid option."); break;
            }

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Executes backup jobs from command-line arguments (non-interactive mode).
    /// Supports ranges like "1-3" and lists like "1;3" or "1,3".
    /// </summary>
    /// <param name="args">Raw command-line arguments.</param>
    public void ExecuteFromArgs(string[] args)
    {
        var jobs = _backupService.GetAllJobs();
        if (jobs.Count == 0)
        {
            PrintWarning(_lang.Get("info_no_jobs"));
            return;
        }

        // Filter out logdir args
        var jobArgs = args.Where(a => !a.StartsWith("--logdir") && !a.StartsWith("-l=")).ToArray();
        var indices = ParseJobIndices(string.Join(" ", jobArgs), jobs.Count);

        if (indices.Count == 0)
        {
            PrintWarning("No valid job indices provided.");
            return;
        }

        var names = indices.Select(i => jobs[i - 1].Name).ToList();
        Console.WriteLine(_lang.Get("info_backing_up"));
        _backupService.ExecuteJobs(names, _logger);
        PrintSuccess(_lang.Get("success_backup_complete"));
    }

    // ─────────────────────────────── Private helpers ───────────────────────────────

    /// <summary>Lists all configured backup jobs.</summary>
    private void ListJobs()
    {
        var jobs = _backupService.GetAllJobs();
        if (jobs.Count == 0)
        {
            Console.WriteLine(_lang.Get("info_no_jobs"));
            return;
        }

        Console.WriteLine($"{"#",-4} {"Name",-25} {"Type",-14} Source → Target");
        Console.WriteLine(new string('─', 80));
        for (int i = 0; i < jobs.Count; i++)
        {
            var j = jobs[i];
            string type = j.Type == BackupType.Full
                ? _lang.Get("job_type_full")
                : _lang.Get("job_type_differential");
            Console.WriteLine($"{i + 1,-4} {j.Name,-25} {type,-14} {j.SourceDirectory} → {j.TargetDirectory}");
        }
    }

    /// <summary>Prompts the user to create a new backup job.</summary>
    private void CreateJob()
    {
        Console.Write($"{_lang.Get("prompt_enter_name")}: ");
        var name = Console.ReadLine()?.Trim() ?? "";

        Console.Write($"{_lang.Get("prompt_enter_source")}: ");
        var source = Console.ReadLine()?.Trim() ?? "";

        Console.Write($"{_lang.Get("prompt_enter_target")}: ");
        var target = Console.ReadLine()?.Trim() ?? "";

        var type = AskBackupType();
        var result = _backupService.CreateJob(name, source, target, type);

        if (result == BackupCreationResult.Success) PrintSuccess(_lang.Get(result.TranslationKey()));
        else PrintError(_lang.Get(result.TranslationKey()));
    }

    /// <summary>Asks the backup type until the user types 1 (full) or 2 (differential).</summary>
    private BackupType AskBackupType()
    {
        Console.WriteLine($"{_lang.Get("prompt_select_type")}:");
        Console.WriteLine($"  1. {_lang.Get("job_type_full")}");
        Console.WriteLine($"  2. {_lang.Get("job_type_differential")}");
        while (true)
        {
            Console.Write(">>> ");
            string? choice = Console.ReadLine()?.Trim();
            if (choice is null or "1") return BackupType.Full; // null: end of input
            if (choice == "2") return BackupType.Differential;
            PrintWarning(_lang.Get("warn_invalid_type"));
        }
    }

    /// <summary>Prompts the user to select and execute one or more backup jobs.</summary>
    private void ExecuteJobs()
    {
        var jobs = _backupService.GetAllJobs();
        if (jobs.Count == 0) { Console.WriteLine(_lang.Get("info_no_jobs")); return; }

        ListJobs();
        Console.WriteLine();
        Console.Write($"{_lang.Get("prompt_select_jobs")} (ex: 1-3, 1;2, all): ");
        var input = Console.ReadLine()?.Trim() ?? "";

        List<int> indices;
        if (input.Equals("all", StringComparison.OrdinalIgnoreCase))
            indices = Enumerable.Range(1, jobs.Count).ToList();
        else
            indices = ParseJobIndices(input, jobs.Count);

        if (indices.Count == 0) { PrintWarning("No valid selection."); return; }

        var names = indices.Select(i => jobs[i - 1].Name).ToList();
        Console.WriteLine();
        Console.WriteLine(_lang.Get("info_backing_up"));
        _backupService.ExecuteJobs(names, _logger);
        PrintSuccess(_lang.Get("success_backup_complete"));
    }

    /// <summary>Prompts the user to delete a backup job by name or number.</summary>
    private void DeleteJob()
    {
        var jobs = _backupService.GetAllJobs();
        if (jobs.Count == 0) { Console.WriteLine(_lang.Get("info_no_jobs")); return; }

        ListJobs();
        Console.WriteLine();
        Console.Write($"{_lang.Get("job_name")} / # : ");
        var input = Console.ReadLine()?.Trim() ?? "";

        string? name = null;
        if (int.TryParse(input, out int idx) && idx >= 1 && idx <= jobs.Count)
            name = jobs[idx - 1].Name;
        else
            name = input;

        var job = _backupService.GetJob(name);
        if (job == null) { PrintError(_lang.Get("error_job_not_found")); return; }

        _backupService.DeleteJob(name);
        PrintSuccess(_lang.Get("success_job_deleted"));
    }

    /// <summary>Shows and allows modifying application settings (language, log format).</summary>
    private void ShowSettings()
    {
        Console.WriteLine($"── {_lang.Get("menu_settings")} ──");
        Console.WriteLine($"  1. {_lang.Get("settings_language")} [{_lang.CurrentLanguage.ToUpper()}]");
        Console.WriteLine($"  2. {_lang.Get("settings_log_format")}");
        Console.WriteLine($"  0. Back");
        Console.Write(">>> ");
        var choice = Console.ReadLine()?.Trim();

        if (choice == "1")
        {
            Console.WriteLine("  1. English");
            Console.WriteLine("  2. Français");
            Console.Write(">>> ");
            var lang = Console.ReadLine()?.Trim();
            _lang.SetLanguage(lang == "2" ? "fr" : "en");
            PrintSuccess("Language updated.");
        }
        else if (choice == "2")
        {
            Console.WriteLine($"  1. {_lang.Get("log_format_json")}");
            Console.WriteLine($"  2. {_lang.Get("log_format_xml")}");
            Console.Write(">>> ");
            var fmt = Console.ReadLine()?.Trim();
            var format = fmt == "2" ? LogFormat.Xml : LogFormat.Json;
            _logger = LoggerFactory.Create(format, _logDirectory);
            PrintSuccess(_lang.Get("settings_log_format") + " → " + format);
        }
    }

    /// <summary>
    /// Parses a job selection string into a list of 1-based indices.
    /// Supports ranges ("1-3"), semicolons ("1;3"), commas ("1,2,3").
    /// </summary>
    private static List<int> ParseJobIndices(string input, int maxCount)
    {
        var result = new HashSet<int>();
        var parts = input.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            if (part.Contains('-'))
            {
                var bounds = part.Split('-');
                if (bounds.Length == 2
                    && int.TryParse(bounds[0], out int from)
                    && int.TryParse(bounds[1], out int to))
                {
                    for (int i = Math.Max(1, from); i <= Math.Min(maxCount, to); i++)
                        result.Add(i);
                }
            }
            else if (int.TryParse(part, out int idx) && idx >= 1 && idx <= maxCount)
            {
                result.Add(idx);
            }
        }

        return result.OrderBy(x => x).ToList();
    }

    // ─────────────────────────────── UI Helpers ───────────────────────────────

    private static void PrintHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║        EasySave v1.1.1 — ProSoft     ║");
        Console.WriteLine("╚══════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static void PrintSuccess(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ {msg}");
        Console.ResetColor();
    }

    private static void PrintError(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"✗ {msg}");
        Console.ResetColor();
    }

    private static void PrintWarning(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⚠ {msg}");
        Console.ResetColor();
    }
}

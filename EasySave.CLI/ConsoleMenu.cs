using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;
using EasyLog;

namespace EasySave.CLI;

/// <summary>
/// Interactive console menu and command-line execution of EasySave 3.0.
/// It works on the same jobs and settings as the graphical interface and applies
/// every v3.0 rule (parallel jobs, encryption, priority files, business software...).
/// </summary>
public sealed class ConsoleMenu
{
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(500);

    private readonly BackupServiceV3 _backupService;
    private readonly StateManager _stateManager;
    private readonly ILanguageProvider _lang;
    private readonly AppSettings _settings;
    private readonly string? _logDirectory;

    /// <summary>
    /// Initializes a new instance of <see cref="ConsoleMenu"/>.
    /// </summary>
    /// <param name="backupService">The v3.0 backup service (shared with the graphical interface).</param>
    /// <param name="stateManager">The real-time state, read to display the progress.</param>
    /// <param name="languageProvider">The language provider for localization.</param>
    /// <param name="settings">The persisted settings (log format, log destination...).</param>
    /// <param name="logDirectory">A local log directory overriding the default one, or <c>null</c>.</param>
    public ConsoleMenu(BackupServiceV3 backupService, StateManager stateManager,
        ILanguageProvider languageProvider, AppSettings settings, string? logDirectory)
    {
        _backupService = backupService;
        _stateManager = stateManager;
        _lang = languageProvider;
        _settings = settings;
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
                default: PrintWarning(_lang.Get("warn_invalid_option")); break;
            }

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Executes backup jobs from command-line arguments (non-interactive mode),
    /// e.g. "1-3" for jobs 1 to 3 or "1;3" for jobs 1 and 3.
    /// </summary>
    /// <param name="jobArgs">The job selection arguments.</param>
    /// <returns>The process exit code: 0 if every selected job completed, 1 otherwise.</returns>
    public int ExecuteFromArgs(string[] jobArgs)
    {
        var jobs = _backupService.GetAllJobs();
        if (jobs.Count == 0)
        {
            PrintWarning(_lang.Get("info_no_jobs"));
            return 1;
        }

        var indices = JobSelectionParser.Parse(string.Join(" ", jobArgs), jobs.Count);
        if (indices.Count == 0)
        {
            PrintWarning(_lang.Get("warn_no_valid_selection"));
            return 1;
        }

        return RunJobs(indices.Select(i => jobs[i - 1].Name).ToList()) ? 0 : 1;
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

        Console.WriteLine($"{"#",-4} {_lang.Get("job_name"),-25} {_lang.Get("job_type"),-18} {_lang.Get("job_source")} → {_lang.Get("job_target")}");
        Console.WriteLine(new string('─', 80));
        for (int i = 0; i < jobs.Count; i++)
        {
            var j = jobs[i];
            string type = j.Type == BackupType.Full
                ? _lang.Get("job_type_full")
                : _lang.Get("job_type_differential");
            Console.WriteLine($"{i + 1,-4} {j.Name,-25} {type,-18} {j.SourceDirectory} → {j.TargetDirectory}");
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

        Console.WriteLine($"{_lang.Get("prompt_select_type")}:");
        Console.WriteLine($"  1. {_lang.Get("job_type_full")}");
        Console.WriteLine($"  2. {_lang.Get("job_type_differential")}");
        Console.Write(">>> ");
        var typeChoice = Console.ReadLine()?.Trim();
        var type = typeChoice == "2" ? BackupType.Differential : BackupType.Full;

        var result = _backupService.CreateJob(name, source, target, type);

        switch (result)
        {
            case BackupCreationResult.Success:
                PrintSuccess(_lang.Get("success_job_created"));
                break;
            case BackupCreationResult.InvalidName:
                PrintError(_lang.Get("error_invalid_name"));
                break;
            case BackupCreationResult.SourceNotFound:
                PrintError(_lang.Get("error_source_not_found"));
                break;
            case BackupCreationResult.TargetNotFound:
                PrintError(_lang.Get("error_target_not_found"));
                break;
            case BackupCreationResult.DuplicateName:
                PrintError(_lang.Get("error_duplicate_name"));
                break;
            default:
                PrintError(_lang.Get("error_unknown"));
                break;
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
        var indices = JobSelectionParser.Parse(Console.ReadLine() ?? "", jobs.Count);

        if (indices.Count == 0) { PrintWarning(_lang.Get("warn_no_valid_selection")); return; }

        Console.WriteLine();
        RunJobs(indices.Select(i => jobs[i - 1].Name).ToList());
    }

    /// <summary>
    /// Runs the given jobs in parallel with the v3.0 rules and shows their progress until they end.
    /// Ctrl+C stops every job immediately.
    /// </summary>
    /// <returns><c>true</c> if every job completed.</returns>
    private bool RunJobs(List<string> names)
    {
        Console.WriteLine(_lang.Get("info_backing_up"));
        Console.WriteLine(_lang.Get("cli_stop_hint"));

        var logger = _settings.CreateLogger(_logDirectory);
        ConsoleCancelEventHandler stopOnCtrlC = (_, e) =>
        {
            e.Cancel = true; // Keep the process alive so that the jobs end cleanly
            _backupService.StopAll();
        };
        Console.CancelKeyPress += stopOnCtrlC;
        try
        {
            var run = _backupService.ExecuteJobsParallelAsync(names, logger);
            string lastProgress = string.Empty;
            while (!run.Wait(ProgressInterval))
                lastProgress = PrintProgress(names, lastProgress);
            run.GetAwaiter().GetResult();
        }
        finally
        {
            Console.CancelKeyPress -= stopOnCtrlC;
            (logger as IDisposable)?.Dispose();
        }

        return PrintResults(names);
    }

    /// <summary>Prints one progress line (only when it changed).</summary>
    /// <returns>The line printed last.</returns>
    private string PrintProgress(List<string> names, string lastProgress)
    {
        var states = _stateManager.GetAllStates().Where(s => names.Contains(s.Name));
        string progress = string.Join(" | ", states.Select(s => $"{s.Name} {s.Progress:0}% {_lang.Get(s.Status.TranslationKey())}"));
        if (_backupService.IsBusinessSoftwareRunning)
            progress += $"  {_lang.Get("status_business_paused")}";

        if (progress != lastProgress)
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {progress}");
        return progress;
    }

    /// <summary>Prints the final status of each job.</summary>
    /// <returns><c>true</c> if every job completed.</returns>
    private bool PrintResults(List<string> names)
    {
        var states = _stateManager.GetAllStates();
        bool allCompleted = true;
        foreach (var name in names)
        {
            var status = states.FirstOrDefault(s => s.Name == name)?.Status ?? JobStatus.Error;
            string line = $"{name}: {_lang.Get(status.TranslationKey())}";
            if (status == JobStatus.Completed) PrintSuccess(line);
            else { PrintWarning(line); allCompleted = false; }
        }

        if (allCompleted) PrintSuccess(_lang.Get("success_backup_complete"));
        else PrintError(_lang.Get("cli_result_failed"));
        return allCompleted;
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

    /// <summary>Shows and allows modifying application settings (language, log format, log destination).</summary>
    private void ShowSettings()
    {
        Console.WriteLine($"── {_lang.Get("menu_settings")} ──");
        Console.WriteLine($"  1. {_lang.Get("settings_language")} [{_lang.CurrentLanguage.ToUpper()}]");
        Console.WriteLine($"  2. {_lang.Get("settings_log_format")} [{_settings.LogFileFormat.ToString().ToUpper()}]");
        Console.WriteLine($"  3. {_lang.Get("lbl_log_mode").TrimEnd(' ', ':')} [{_lang.Get(LogModeKey(_settings.LogMode))}]");
        Console.WriteLine($"  0. {_lang.Get("menu_back")}");
        Console.Write(">>> ");
        var choice = Console.ReadLine()?.Trim();

        if (choice == "1")
        {
            Console.WriteLine("  1. English");
            Console.WriteLine("  2. Français");
            Console.Write(">>> ");
            var lang = Console.ReadLine()?.Trim();
            _settings.Language = lang == "2" ? "fr" : "en"; // Shared with the graphical interface
            _lang.SetLanguage(_settings.Language);
            PrintSuccess(_lang.Get("success_language_updated"));
        }
        else if (choice == "2")
        {
            Console.WriteLine($"  1. {_lang.Get("log_format_json")}");
            Console.WriteLine($"  2. {_lang.Get("log_format_xml")}");
            Console.Write(">>> ");
            var fmt = Console.ReadLine()?.Trim();
            var format = fmt == "2" ? LogFormat.Xml : LogFormat.Json;
            _settings.LogFormat = format.ToString(); // Shared with the graphical interface
            PrintSuccess(_lang.Get("settings_log_format") + " → " + format);
        }
        else if (choice == "3")
        {
            var modes = Enum.GetValues<LogMode>();
            for (int i = 0; i < modes.Length; i++)
                Console.WriteLine($"  {i + 1}. {_lang.Get(LogModeKey(modes[i]))}");
            Console.Write(">>> ");
            if (int.TryParse(Console.ReadLine()?.Trim(), out int m) && m >= 1 && m <= modes.Length)
            {
                _settings.LogMode = modes[m - 1];
                PrintSuccess(_lang.Get(LogModeKey(_settings.LogMode)));
            }
        }
    }

    private static string LogModeKey(LogMode mode) => mode switch
    {
        LogMode.Centralized => "log_mode_centralized",
        LogMode.Both        => "log_mode_both",
        _                   => "log_mode_local"
    };

    // ─────────────────────────────── UI Helpers ───────────────────────────────

    private static void PrintHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║        EasySave v3.0 — ProSoft       ║");
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

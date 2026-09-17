using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasyLog;

namespace EasySave.Core.Services;

/// <summary>
/// v3.0 backup service: parallel execution, Play/Pause/Stop per job,
/// priority file rule, large file bandwidth throttle,
/// and automatic pause/resume on business software detection.
/// </summary>
public sealed class BackupServiceV3 : IDisposable
{
    // ── Constants ──────────────────────────────────────────────────────────────
    private static readonly Regex NameRegex = new(@"^[\w\s\-]{1,100}$", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _configPath;
    private readonly List<BackupJob> _jobs;
    private readonly StateManager _stateManager;
    private readonly AppSettings _appSettings;
    private readonly BusinessSoftwareMonitor _monitor;
    private readonly object _lock = new();

    // ── Per-job controllers (Pause / Stop) ────────────────────────────────────
    private readonly Dictionary<string, JobController> _controllers = [];

    // ── Business software auto-pause ─────────────────────────────────────────
    private readonly System.Threading.Timer _businessTimer;

    // ── Constructor ────────────────────────────────────────────────────────────
    public BackupServiceV3(StateManager stateManager, AppSettings appSettings,
        BusinessSoftwareMonitor monitor, string? configPath = null)
    {
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _appSettings  = appSettings  ?? throw new ArgumentNullException(nameof(appSettings));
        _monitor      = monitor      ?? throw new ArgumentNullException(nameof(monitor));
        _configPath   = configPath   ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "EasySave", "backup_config_v3.json");
        _jobs = LoadConfig();

        // Poll for business software every 500ms and auto-pause/resume running jobs
        _businessTimer = new System.Threading.Timer(PollBusinessSoftware, null,
            TimeSpan.Zero, TimeSpan.FromMilliseconds(500));
    }

    // ── Public API (mirrors IBackupService) ───────────────────────────────────

    public BackupCreationResult CreateJob(string name, string source, string target, BackupType type)
    {
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(name) || !NameRegex.IsMatch(name))
                return BackupCreationResult.InvalidName;
            if (_jobs.Exists(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return BackupCreationResult.DuplicateName;
            if (!Directory.Exists(source))
                return BackupCreationResult.SourceNotFound;

            string? targetParent = Path.GetDirectoryName(target.TrimEnd(Path.DirectorySeparatorChar));
            if (!string.IsNullOrEmpty(targetParent) && !Directory.Exists(targetParent))
                return BackupCreationResult.TargetNotFound;

            _jobs.Add(new BackupJob { Name = name, SourceDirectory = source, TargetDirectory = target, Type = type });
            SaveConfig();
            return BackupCreationResult.Success;
        }
    }

    public void DeleteJob(string name)
    {
        lock (_lock)
        {
            int removed = _jobs.RemoveAll(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (removed > 0) { _stateManager.RemoveJobState(name); SaveConfig(); }
        }
    }

    public List<BackupJob> GetAllJobs() { lock (_lock) return [.. _jobs]; }

    public BackupJob? GetJob(string name)
    {
        lock (_lock) return _jobs.Find(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    // ── Parallel execution ────────────────────────────────────────────────────

    /// <summary>
    /// Launches all specified jobs in parallel. Returns a Task that completes
    /// when all jobs finish (or are stopped/cancelled).
    /// </summary>
    public Task ExecuteJobsParallelAsync(IEnumerable<string> names, ILogger logger)
    {
        var jobList = names
            .Select(GetJob)
            .Where(j => j is not null)
            .Cast<BackupJob>()
            .ToList();

        if (jobList.Count == 0) return Task.CompletedTask;

        // Create fresh controllers for each job
        lock (_lock)
        {
            foreach (var job in jobList)
            {
                _controllers.TryGetValue(job.Name, out var existing);
                existing?.Dispose();
                _controllers[job.Name] = new JobController();
            }
        }

        var tasks = jobList.Select(job =>
            Task.Run(() => RunBackupAsync(job, logger, _controllers[job.Name])));

        return Task.WhenAll(tasks);
    }

    // ── Play / Pause / Stop (per job) ─────────────────────────────────────────

    public void PauseJob(string name)
    {
        if (_controllers.TryGetValue(name, out var ctrl)) ctrl.Pause();
    }

    public void ResumeJob(string name)
    {
        if (_controllers.TryGetValue(name, out var ctrl)) ctrl.Resume();
    }

    public void StopJob(string name)
    {
        if (_controllers.TryGetValue(name, out var ctrl)) ctrl.Stop();
    }

    public void PauseAll()  { lock (_lock) foreach (var c in _controllers.Values) c.Pause(); }
    public void ResumeAll() { lock (_lock) foreach (var c in _controllers.Values) c.Resume(); }
    public void StopAll()   { lock (_lock) foreach (var c in _controllers.Values) c.Stop(); }

    // ── Business software auto-pause polling ──────────────────────────────────

    private bool _wasBusinessRunning;

    private void PollBusinessSoftware(object? _)
    {
        bool isRunning = _monitor.IsRunning();

        if (isRunning && !_wasBusinessRunning)
        {
            // Business software just appeared → pause all
            PauseAll();
            _wasBusinessRunning = true;
        }
        else if (!isRunning && _wasBusinessRunning)
        {
            // Business software just closed → resume all
            ResumeAll();
            _wasBusinessRunning = false;
        }
    }

    // ── Private: backup engine ────────────────────────────────────────────────

    private async Task RunBackupAsync(BackupJob job, ILogger logger, JobController ctrl)
    {
        var ct = ctrl.Token;
        Directory.CreateDirectory(job.TargetDirectory);

        var allFiles  = GetEligibleFiles(job);
        long totalSize = allFiles.Sum(f => new FileInfo(f).Length);

        var priorityExtensions = _appSettings.PriorityExtensions
            .Select(e => e.ToLowerInvariant()).ToHashSet();
        var encExtensions = _appSettings.EncryptionExtensions
            .Select(e => e.ToLowerInvariant()).ToHashSet();

        var state = new JobState
        {
            Name           = job.Name,
            Status         = JobStatus.Active,
            TotalFiles     = allFiles.Count,
            TotalSize      = totalSize,
            RemainingFiles = allFiles.Count,
            RemainingSize  = totalSize,
            Progress       = 0,
            LastActionTime = DateTime.Now
        };
        _stateManager.UpdateJobState(state);

        int  processed     = 0;
        long processedSize = 0;

        foreach (string sourceFile in allFiles)
        {
            try
            {
                // ── 1. Check Pause / Stop between files ───────────────────────
                state.Status = JobStatus.Paused;
                _stateManager.UpdateJobState(state);
                ctrl.CheckPauseOrStop();
                state.Status = JobStatus.Active;

                // ── 2. Priority rule ──────────────────────────────────────────
                string ext        = Path.GetExtension(sourceFile).ToLowerInvariant();
                bool   isPriority = priorityExtensions.Count > 0 && priorityExtensions.Contains(ext);
                await PriorityTransferGuard.Instance.AcquireAsync(isPriority, ct);

                // ── 3. Large-file bandwidth throttle ──────────────────────────
                var fileInfo = new FileInfo(sourceFile);
                using var largeSlot = await LargeFileTransferGuard.Instance
                    .AcquireAsync(fileInfo.Length, _appSettings.MaxParallelFileSizeKb, ct);

                // ── 4. Transfer ───────────────────────────────────────────────
                string relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
                string targetFile   = Path.Combine(job.TargetDirectory, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

                state.CurrentSourceFile = LogEntry.ToUncPath(sourceFile);
                state.CurrentTargetFile = LogEntry.ToUncPath(targetFile);
                state.LastActionTime    = DateTime.Now;
                _stateManager.UpdateJobState(state);

                long transferMs  = -1;
                long encryptMs   = 0;

                var sw = Stopwatch.StartNew();
                try { File.Copy(sourceFile, targetFile, overwrite: true); sw.Stop(); transferMs = sw.ElapsedMilliseconds; }
                catch { sw.Stop(); transferMs = -1; }

                // ── 5. Encryption ─────────────────────────────────────────────
                if (transferMs >= 0 && encExtensions.Count > 0 && encExtensions.Contains(ext))
                    encryptMs = RunCryptoSoft(targetFile);

                // ── 6. Release priority slot ──────────────────────────────────
                if (isPriority) PriorityTransferGuard.Instance.ReleasePriority();

                // ── 7. Log ────────────────────────────────────────────────────
                logger.Log(new LogEntry
                {
                    Timestamp           = DateTime.Now,
                    BackupName          = job.Name,
                    SourceFilePath      = sourceFile,
                    TargetFilePath      = targetFile,
                    FileSize            = fileInfo.Length,
                    TransferDurationMs  = transferMs,
                    EncryptionDurationMs = encryptMs
                });

                processed++;
                processedSize        += fileInfo.Length;
                state.RemainingFiles  = allFiles.Count - processed;
                state.RemainingSize   = totalSize - processedSize;
                state.Progress        = totalSize > 0 ? processedSize * 100.0 / totalSize : 100.0;
                state.LastActionTime  = DateTime.Now;
                _stateManager.UpdateJobState(state);
            }
            catch (OperationCanceledException)
            {
                state.Status = JobStatus.Inactive;
                _stateManager.UpdateJobState(state);
                return;
            }
        }

        state.Status           = JobStatus.Completed;
        state.Progress         = 100;
        state.RemainingFiles   = 0;
        state.RemainingSize    = 0;
        state.CurrentSourceFile = string.Empty;
        state.CurrentTargetFile = string.Empty;
        state.LastActionTime   = DateTime.Now;
        _stateManager.UpdateJobState(state);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static long RunCryptoSoft(string targetFile)
    {
        string? cryptoSoftPath = FindCryptoSoft();
        if (cryptoSoftPath is null) return -10;

        string tempFile = targetFile + ".tmp";
        try
        {
            var psi = new ProcessStartInfo(cryptoSoftPath, $"\"{targetFile}\" \"{tempFile}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            using var process = Process.Start(psi)!;
            process.WaitForExit(30_000);
            int exitCode = process.ExitCode;
            if (exitCode > 0) { File.Move(tempFile, targetFile, overwrite: true); return exitCode; }
            return exitCode;
        }
        catch { if (File.Exists(tempFile)) File.Delete(tempFile); return -11; }
    }

    private static string? FindCryptoSoft()
    {
        string exeDir   = AppContext.BaseDirectory;
        string candidate = Path.Combine(exeDir, "CryptoSoft.exe");
        if (File.Exists(candidate)) return candidate;

        var dir = new DirectoryInfo(exeDir);
        while (dir is not null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
            {
                foreach (var p in new[]
                {
                    Path.Combine(dir.FullName, "CryptoSoft", "bin", "Debug",   "net8.0", "CryptoSoft.exe"),
                    Path.Combine(dir.FullName, "CryptoSoft", "bin", "Release",  "net8.0", "CryptoSoft.exe"),
                })
                    if (File.Exists(p)) return p;
                break;
            }
            dir = dir.Parent;
        }
        return null;
    }

    private static List<string> GetEligibleFiles(BackupJob job)
    {
        var all = Directory.EnumerateFiles(job.SourceDirectory, "*", SearchOption.AllDirectories).ToList();
        if (job.Type == BackupType.Full) return all;
        return all.Where(src =>
        {
            string rel = Path.GetRelativePath(job.SourceDirectory, src);
            string tgt = Path.Combine(job.TargetDirectory, rel);
            return !File.Exists(tgt) || File.GetLastWriteTimeUtc(src) > File.GetLastWriteTimeUtc(tgt);
        }).ToList();
    }

    private List<BackupJob> LoadConfig()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
            if (!File.Exists(_configPath)) return [];
            return JsonSerializer.Deserialize<List<BackupJob>>(File.ReadAllText(_configPath), JsonOptions) ?? [];
        }
        catch { return []; }
    }

    private void SaveConfig() =>
        File.WriteAllText(_configPath, JsonSerializer.Serialize(_jobs, JsonOptions));

    public void Dispose()
    {
        _businessTimer.Dispose();
        lock (_lock) foreach (var c in _controllers.Values) c.Dispose();
    }
}

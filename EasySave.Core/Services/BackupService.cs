using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasyLog;

namespace EasySave.Core.Services;

/// <summary>
/// Manages backup job configuration and orchestrates backup execution.
/// Enforces a strict limit of 5 jobs for v1.0.
/// Thread-safe.
/// </summary>
public sealed class BackupService : IBackupService
{
    // ── Constants ──────────────────────────────────────────────────────────────
    private const int MaxJobs = 5;
    private readonly string _configPath;
    private static readonly Regex NameRegex = new(@"^[\w\s\-]{1,50}$", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // ── State ──────────────────────────────────────────────────────────────────
    private readonly List<BackupJob> _jobs;
    private readonly StateManager _stateManager;
    private readonly object _lock = new();

    // ── Constructor ────────────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new <see cref="BackupService"/> and loads persisted job configuration.
    /// </summary>
    /// <param name="stateManager">The state manager used to persist real-time job states.</param>
    /// <param name="configPath">Optional custom config path for testing.</param>
    public BackupService(StateManager stateManager, string? configPath = null)
    {
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _configPath = configPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "EasySave", "backup_config.json");
        _jobs = LoadConfig();
    }

    // ── IBackupService ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public BackupCreationResult CreateJob(string name, string sourceDirectory,
        string targetDirectory, BackupType type)
    {
        lock (_lock)
        {
            if (_jobs.Count >= MaxJobs)
                return BackupCreationResult.MaxJobsExceeded;

            if (string.IsNullOrWhiteSpace(name) || !NameRegex.IsMatch(name))
                return BackupCreationResult.InvalidName;

            if (_jobs.Exists(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return BackupCreationResult.DuplicateName;

            if (!Directory.Exists(sourceDirectory))
                return BackupCreationResult.SourceNotFound;

            // Target itself may not exist yet, but its parent must
            string? targetParent = Path.GetDirectoryName(targetDirectory.TrimEnd(Path.DirectorySeparatorChar));
            if (!string.IsNullOrEmpty(targetParent) && !Directory.Exists(targetParent))
                return BackupCreationResult.TargetNotFound;

            _jobs.Add(new BackupJob
            {
                Name = name,
                SourceDirectory = sourceDirectory,
                TargetDirectory = targetDirectory,
                Type = type
            });

            SaveConfig();
            return BackupCreationResult.Success;
        }
    }

    /// <inheritdoc/>
    public void DeleteJob(string name)
    {
        lock (_lock)
        {
            int removed = _jobs.RemoveAll(j =>
                j.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (removed > 0)
            {
                _stateManager.RemoveJobState(name);
                SaveConfig();
            }
        }
    }

    /// <inheritdoc/>
    public List<BackupJob> GetAllJobs()
    {
        lock (_lock) return [.. _jobs];
    }

    /// <inheritdoc/>
    public BackupJob? GetJob(string name)
    {
        lock (_lock)
            return _jobs.Find(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public void ExecuteJob(string name, ILogger logger)
    {
        BackupJob? job = GetJob(name);
        if (job is null) return;
        RunBackup(job, logger);
    }

    /// <inheritdoc/>
    public void ExecuteJobs(IEnumerable<string> names, ILogger logger)
    {
        foreach (var name in names)
            ExecuteJob(name, logger);
    }

    // ── Private: backup engine ─────────────────────────────────────────────────

    /// <summary>Executes a single backup job, logging each file transfer.</summary>
    private void RunBackup(BackupJob job, ILogger logger)
    {
        Directory.CreateDirectory(job.TargetDirectory);

        var allFiles = GetEligibleFiles(job);
        long totalSize = allFiles.Sum(f => new FileInfo(f).Length);

        // Initialize real-time state
        var state = new JobState
        {
            Name = job.Name,
            Status = JobStatus.Active,
            TotalFiles = allFiles.Count,
            TotalSize = totalSize,
            RemainingFiles = allFiles.Count,
            RemainingSize = totalSize,
            Progress = 0,
            LastActionTime = DateTime.Now
        };
        _stateManager.UpdateJobState(state);

        int processed = 0;
        long processedSize = 0;

        foreach (string sourceFile in allFiles)
        {
            var fileInfo = new FileInfo(sourceFile);
            string relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
            string targetFile = Path.Combine(job.TargetDirectory, relativePath);

            // Ensure sub-directory exists in target
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

            // Update state: file being transferred
            state.CurrentSourceFile = LogEntry.ToUncPath(sourceFile);
            state.CurrentTargetFile = LogEntry.ToUncPath(targetFile);
            state.LastActionTime = DateTime.Now;
            _stateManager.UpdateJobState(state);

            long transferMs;
            var sw = Stopwatch.StartNew();
            try
            {
                File.Copy(sourceFile, targetFile, overwrite: true);
                sw.Stop();
                transferMs = sw.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                sw.Stop();
                transferMs = -1; // Negative = error
                Console.Error.WriteLine($"[EasySave] Error copying '{sourceFile}': {ex.Message}");
            }

            // Log the transfer
            logger.Log(new LogEntry
            {
                Timestamp = DateTime.Now,
                BackupName = job.Name,
                SourceFilePath = sourceFile,
                TargetFilePath = targetFile,
                FileSize = fileInfo.Length,
                TransferDurationMs = transferMs,
                EncryptionDurationMs = 0
            });

            // Update progress
            processed++;
            processedSize += fileInfo.Length;
            state.RemainingFiles = allFiles.Count - processed;
            state.RemainingSize = totalSize - processedSize;
            state.Progress = totalSize > 0 ? processedSize * 100.0 / totalSize : 100.0;
            state.LastActionTime = DateTime.Now;
            _stateManager.UpdateJobState(state);
        }

        // Mark as completed
        state.Status = JobStatus.Completed;
        state.Progress = 100;
        state.RemainingFiles = 0;
        state.RemainingSize = 0;
        state.CurrentSourceFile = string.Empty;
        state.CurrentTargetFile = string.Empty;
        state.LastActionTime = DateTime.Now;
        _stateManager.UpdateJobState(state);
    }

    /// <summary>
    /// Returns the list of source files eligible for the given job.
    /// Full backup: all files. Differential: only new or modified files.
    /// </summary>
    private static List<string> GetEligibleFiles(BackupJob job)
    {
        var allSourceFiles = Directory
            .EnumerateFiles(job.SourceDirectory, "*", SearchOption.AllDirectories)
            .ToList();

        if (job.Type == BackupType.Full)
            return allSourceFiles;

        // Differential: include only files newer than their target counterpart
        return allSourceFiles.Where(src =>
        {
            string relative = Path.GetRelativePath(job.SourceDirectory, src);
            string target = Path.Combine(job.TargetDirectory, relative);
            if (!File.Exists(target)) return true;
            return File.GetLastWriteTimeUtc(src) > File.GetLastWriteTimeUtc(target);
        }).ToList();
    }

    // ── Config persistence ─────────────────────────────────────────────────────

    private List<BackupJob> LoadConfig()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
            if (!File.Exists(_configPath)) return [];
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<List<BackupJob>>(json, JsonOptions) ?? [];
        }
        catch { return []; }
    }

    private void SaveConfig()
    {
        var json = JsonSerializer.Serialize(_jobs, JsonOptions);
        File.WriteAllText(_configPath, json);
    }
}

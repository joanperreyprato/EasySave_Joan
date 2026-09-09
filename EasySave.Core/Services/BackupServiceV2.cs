using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasyLog;

namespace EasySave.Core.Services;

/// <summary>
/// v2.0 backup service: unlimited jobs, CryptoSoft encryption, business software detection.
/// Inherits all base logic from <see cref="BackupService"/> but removes the 5-job limit
/// and adds encryption support.
/// </summary>
public sealed class BackupServiceV2 : IBackupService
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

    // ── Constructor ────────────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new <see cref="BackupServiceV2"/> with all v2.0 dependencies.
    /// </summary>
    public BackupServiceV2(StateManager stateManager, AppSettings appSettings,
        BusinessSoftwareMonitor monitor, string? configPath = null)
    {
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _configPath = configPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "EasySave", "backup_config_v2.json");
        _jobs = LoadConfig();
    }

    // ── IBackupService ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>v2.0: No maximum job limit.</remarks>
    public BackupCreationResult CreateJob(string name, string sourceDirectory,
        string targetDirectory, BackupType type)
    {
        lock (_lock)
        {
            // v2.0: No job limit — unlimited jobs
            if (string.IsNullOrWhiteSpace(name) || !NameRegex.IsMatch(name))
                return BackupCreationResult.InvalidName;

            if (_jobs.Exists(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return BackupCreationResult.DuplicateName;

            if (!Directory.Exists(sourceDirectory))
                return BackupCreationResult.SourceNotFound;

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
            if (removed > 0) { _stateManager.RemoveJobState(name); SaveConfig(); }
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
        var job = GetJob(name);
        if (job is null) return;

        // v2.0: Block execution if business software is running
        if (_monitor.IsRunning())
        {
            logger.Log(new LogEntry
            {
                Timestamp = DateTime.Now,
                BackupName = name,
                SourceFilePath = string.Empty,
                TargetFilePath = string.Empty,
                FileSize = 0,
                TransferDurationMs = -100, // Convention: blocked by business software
                EncryptionDurationMs = 0
            });
            throw new InvalidOperationException(
                $"Backup blocked: business software '{_appSettings.BusinessSoftwareName}' is running.");
        }

        RunBackup(job, logger);
    }

    /// <inheritdoc/>
    public void ExecuteJobs(IEnumerable<string> names, ILogger logger)
    {
        foreach (var name in names)
        {
            // Re-check before each job in sequential mode
            if (_monitor.IsRunning())
                throw new InvalidOperationException(
                    $"Backup stopped: business software '{_appSettings.BusinessSoftwareName}' is running.");
            ExecuteJob(name, logger);
        }
    }

    // ── Private: backup engine ─────────────────────────────────────────────────

    private void RunBackup(BackupJob job, ILogger logger)
    {
        Directory.CreateDirectory(job.TargetDirectory);

        var allFiles = GetEligibleFiles(job);
        long totalSize = allFiles.Sum(f => new FileInfo(f).Length);

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
        var encExtensions = _appSettings.EncryptionExtensions
            .Select(e => e.ToLowerInvariant()).ToHashSet();

        foreach (string sourceFile in allFiles)
        {
            var fileInfo = new FileInfo(sourceFile);
            string relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
            string targetFile = Path.Combine(job.TargetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

            state.CurrentSourceFile = LogEntry.ToUncPath(sourceFile);
            state.CurrentTargetFile = LogEntry.ToUncPath(targetFile);
            state.LastActionTime = DateTime.Now;
            _stateManager.UpdateJobState(state);

            long transferMs = -1;
            long encryptionMs = 0;

            // Transfer file
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
                transferMs = -1;
                Console.Error.WriteLine($"[EasySave] Copy error '{sourceFile}': {ex.Message}");
            }

            // Encrypt file via CryptoSoft if extension matches
            string ext = Path.GetExtension(sourceFile).ToLowerInvariant();
            if (transferMs >= 0 && encExtensions.Count > 0 && encExtensions.Contains(ext))
            {
                encryptionMs = RunCryptoSoft(targetFile);
            }

            logger.Log(new LogEntry
            {
                Timestamp = DateTime.Now,
                BackupName = job.Name,
                SourceFilePath = sourceFile,
                TargetFilePath = targetFile,
                FileSize = fileInfo.Length,
                TransferDurationMs = transferMs,
                EncryptionDurationMs = encryptionMs
            });

            processed++;
            processedSize += fileInfo.Length;
            state.RemainingFiles = allFiles.Count - processed;
            state.RemainingSize = totalSize - processedSize;
            state.Progress = totalSize > 0 ? processedSize * 100.0 / totalSize : 100.0;
            state.LastActionTime = DateTime.Now;
            _stateManager.UpdateJobState(state);
        }

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
    /// Runs CryptoSoft.exe on the given file in-place (encrypts destination file).
    /// Returns the processing time in ms (positive), or a negative error code.
    /// </summary>
    private static long RunCryptoSoft(string targetFile)
    {
        string? cryptoSoftPath = FindCryptoSoft();
        if (cryptoSoftPath is null) return -10; // CryptoSoft not found

        string tempFile = targetFile + ".tmp";
        try
        {
            var psi = new ProcessStartInfo(cryptoSoftPath, $"\"{targetFile}\" \"{tempFile}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi)!;
            process.WaitForExit(30_000); // 30s timeout

            int exitCode = process.ExitCode;
            if (exitCode > 0)
            {
                // Replace target with encrypted version atomically
                File.Move(tempFile, targetFile, overwrite: true);
                return exitCode;
            }
            return exitCode; // Negative = error
        }
        catch
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
            return -11;
        }
    }

    /// <summary>Locates CryptoSoft.exe relative to the current executable.</summary>
    private static string? FindCryptoSoft()
    {
        // Look next to the running executable first
        string exeDir = AppContext.BaseDirectory;
        string candidate = Path.Combine(exeDir, "CryptoSoft.exe");
        if (File.Exists(candidate)) return candidate;

        // Also look in sibling directories (useful during development)
        string? solutionRoot = FindSolutionRoot(exeDir);
        if (solutionRoot is not null)
        {
            var devPaths = new[]
            {
                Path.Combine(solutionRoot, "CryptoSoft", "bin", "Debug", "net8.0", "CryptoSoft.exe"),
                Path.Combine(solutionRoot, "CryptoSoft", "bin", "Release", "net8.0", "CryptoSoft.exe"),
            };
            foreach (var p in devPaths)
                if (File.Exists(p)) return p;
        }

        return null;
    }

    private static string? FindSolutionRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir is not null)
        {
            if (dir.GetFiles("*.sln").Length > 0) return dir.FullName;
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
            if (!File.Exists(tgt)) return true;
            return File.GetLastWriteTimeUtc(src) > File.GetLastWriteTimeUtc(tgt);
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

    private void SaveConfig()
    {
        File.WriteAllText(_configPath, JsonSerializer.Serialize(_jobs, JsonOptions));
    }
}

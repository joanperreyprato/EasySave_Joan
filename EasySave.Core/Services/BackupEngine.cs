using System.Diagnostics;
using EasySave.Core.Models;
using EasyLog;

namespace EasySave.Core.Services;

/// <summary>
/// Runs one backup job: selects the files, copies them, encrypts them if required,
/// writes the daily log and keeps the real-time state up to date.
/// The same engine serves every version; <see cref="BackupRunOptions"/> enables the
/// v2.0 and v3.0 features.
/// </summary>
public sealed class BackupEngine
{
    private const int CopyBufferSize = 81920;

    private readonly StateManager _stateManager;
    private readonly PriorityTransferGuard _priorityGuard;
    private readonly LargeFileTransferGuard _largeFileGuard;

    /// <summary>Initializes the engine.</summary>
    /// <param name="stateManager">Writes the real-time state file.</param>
    /// <param name="priorityGuard">Priority rule shared by all jobs (defaults to the application-wide instance).</param>
    /// <param name="largeFileGuard">Large file rule shared by all jobs (defaults to the application-wide instance).</param>
    public BackupEngine(StateManager stateManager, PriorityTransferGuard? priorityGuard = null,
        LargeFileTransferGuard? largeFileGuard = null)
    {
        _stateManager   = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _priorityGuard  = priorityGuard ?? PriorityTransferGuard.Instance;
        _largeFileGuard = largeFileGuard ?? LargeFileTransferGuard.Instance;
    }

    // ── Planning ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Lists the eligible files of <paramref name="job"/> (priority files first) and registers
    /// its priority files. Every plan must then be passed to <see cref="RunAsync"/>, which releases them.
    /// </summary>
    public BackupPlan Prepare(BackupJob job, IReadOnlySet<string> priorityExtensions)
    {
        BackupPlan plan;
        try
        {
            var files = GetEligibleFiles(job)
                .Select(path => new PlannedFile(path, new FileInfo(path).Length,
                    priorityExtensions.Contains(Path.GetExtension(path))))
                .OrderByDescending(f => f.IsPriority) // Stable sort: keeps the directory order otherwise
                .ToList();
            plan = new BackupPlan(job, files, Error: null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            plan = new BackupPlan(job, [], ex.Message);
        }

        _priorityGuard.Register(plan.PriorityFileCount);
        return plan;
    }

    /// <summary>Full backup: every file. Differential: files missing or newer than their copy.</summary>
    private static IEnumerable<string> GetEligibleFiles(BackupJob job)
    {
        var all = Directory.EnumerateFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
        if (job.Type == BackupType.Full) return all;

        return all.Where(source =>
        {
            string target = GetTargetPath(job, source);
            return !File.Exists(target) || File.GetLastWriteTimeUtc(source) > File.GetLastWriteTimeUtc(target);
        });
    }

    // ── Execution ──────────────────────────────────────────────────────────────

    /// <summary>Transfers every file of the plan.</summary>
    public async Task<BackupOutcome> RunAsync(BackupPlan plan, ILogger logger, BackupRunOptions options)
    {
        int pendingPriority = plan.PriorityFileCount;
        var state = CreateState(plan);
        try
        {
            if (plan.Error is not null)
                return Finish(state, JobStatus.Error, BackupOutcome.Failed);

            EnsureDirectory(plan.Job.TargetDirectory, plan.Job.Name, logger);
            foreach (var file in plan.Files)
            {
                WaitWhilePaused(options.Controller, state);
                if (options.InterruptRequested?.Invoke() == true)
                {
                    LogEvent(logger, plan.Job.Name, LogActions.BusinessSoftwareInterrupted);
                    return Finish(state, JobStatus.Stopped, BackupOutcome.Interrupted);
                }

                await TransferAsync(plan.Job, file, logger, options, state);
                if (file.IsPriority)
                {
                    _priorityGuard.Release();
                    pendingPriority--;
                }
                ReportProgress(state, file.Size);
            }
            return Finish(state, JobStatus.Completed, BackupOutcome.Completed);
        }
        catch (OperationCanceledException)
        {
            LogEvent(logger, plan.Job.Name, LogActions.JobStopped);
            return Finish(state, JobStatus.Stopped, BackupOutcome.Stopped);
        }
        finally
        {
            // Never leave priority files registered: other jobs would wait forever
            _priorityGuard.Release(pendingPriority);
        }
    }

    /// <summary>Writes an event (blocked, paused, stopped...) that is not a file transfer to the log.</summary>
    public static void LogEvent(ILogger logger, string jobName, string action)
        => logger.Log(new LogEntry { Timestamp = DateTime.Now, BackupName = jobName, Action = action });

    private async Task TransferAsync(BackupJob job, PlannedFile file, ILogger logger,
        BackupRunOptions options, JobState state)
    {
        var ct = options.Controller?.Token ?? CancellationToken.None;
        if (!file.IsPriority)
            await _priorityGuard.WaitUntilNoPriorityPendingAsync(ct);

        using var largeFileSlot = await _largeFileGuard.AcquireAsync(file.Size, options.LargeFileThresholdKb, ct);

        string targetFile = GetTargetPath(job, file.Path);
        EnsureDirectory(Path.GetDirectoryName(targetFile)!, job.Name, logger);

        state.CurrentSourceFile = LogEntry.ToUncPath(file.Path);
        state.CurrentTargetFile = LogEntry.ToUncPath(targetFile);
        Save(state, JobStatus.Active);

        long transferMs   = await CopyFileAsync(file.Path, targetFile, ct);
        long encryptionMs = transferMs >= 0 && options.EncryptionExtensions.Contains(Path.GetExtension(file.Path))
            ? await CryptoSoftRunner.EncryptAsync(targetFile)
            : 0;

        logger.Log(new LogEntry
        {
            Timestamp            = DateTime.Now,
            BackupName           = job.Name,
            SourceFilePath       = file.Path,
            TargetFilePath       = targetFile,
            FileSize             = file.Size,
            TransferDurationMs   = transferMs,
            EncryptionDurationMs = encryptionMs
        });
    }

    /// <summary>Copies a file; the copy is cancelled immediately (and the partial file removed) on Stop.</summary>
    /// <returns>The transfer time in ms, or -1 on error.</returns>
    private static async Task<long> CopyFileAsync(string source, string target, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, CopyBufferSize, useAsync: true))
            await using (var output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, CopyBufferSize, useAsync: true))
                await input.CopyToAsync(output, CopyBufferSize, ct);

            File.SetLastWriteTimeUtc(target, File.GetLastWriteTimeUtc(source)); // Same as File.Copy
            return stopwatch.ElapsedMilliseconds;
        }
        catch (OperationCanceledException)
        {
            TryDelete(target);
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return -1;
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string GetTargetPath(BackupJob job, string sourceFile)
        => Path.Combine(job.TargetDirectory, Path.GetRelativePath(job.SourceDirectory, sourceFile));

    /// <summary>Creates a target directory and logs the creation (nothing is logged if it already exists).</summary>
    private static void EnsureDirectory(string directory, string jobName, ILogger logger)
    {
        if (Directory.Exists(directory)) return;
        try
        {
            Directory.CreateDirectory(directory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return; // The file copy will fail and be logged with a negative transfer time
        }

        logger.Log(new LogEntry
        {
            Timestamp      = DateTime.Now,
            BackupName     = jobName,
            Action         = LogActions.DirectoryCreated,
            TargetFilePath = directory
        });
    }

    /// <summary>Blocks while the job is paused; the state shows "Paused" only during a real pause.</summary>
    private void WaitWhilePaused(JobController? controller, JobState state)
    {
        if (controller is null) return;
        controller.Token.ThrowIfCancellationRequested();
        if (!controller.IsPaused) return;

        Save(state, JobStatus.Paused);
        controller.WaitWhilePaused();
        Save(state, JobStatus.Active);
    }

    private JobState CreateState(BackupPlan plan)
    {
        var state = new JobState
        {
            Name           = plan.Job.Name,
            TotalFiles     = plan.Files.Count,
            TotalSize      = plan.TotalSize,
            RemainingFiles = plan.Files.Count,
            RemainingSize  = plan.TotalSize
        };
        Save(state, JobStatus.Active);
        return state;
    }

    private void ReportProgress(JobState state, long transferredSize)
    {
        state.RemainingFiles--;
        state.RemainingSize -= transferredSize;
        state.Progress = state.TotalSize > 0
            ? (state.TotalSize - state.RemainingSize) * 100.0 / state.TotalSize
            : (state.TotalFiles - state.RemainingFiles) * 100.0 / Math.Max(1, state.TotalFiles);
        Save(state, JobStatus.Active);
    }

    private BackupOutcome Finish(JobState state, JobStatus status, BackupOutcome outcome)
    {
        if (status == JobStatus.Completed)
        {
            state.Progress       = 100;
            state.RemainingFiles = 0;
            state.RemainingSize  = 0;
        }
        state.CurrentSourceFile = string.Empty;
        state.CurrentTargetFile = string.Empty;
        Save(state, status);
        return outcome;
    }

    private void Save(JobState state, JobStatus status)
    {
        state.Status         = status;
        state.LastActionTime = DateTime.Now;
        _stateManager.UpdateJobState(state);
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* Best effort */ }
    }
}

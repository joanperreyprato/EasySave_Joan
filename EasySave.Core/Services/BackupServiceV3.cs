using EasySave.Core.Models;
using EasyLog;

namespace EasySave.Core.Services;

/// <summary>
/// v3.0 backup service: parallel execution, Play/Pause/Stop per job or for all jobs,
/// priority files, one large file at a time, and automatic pause/resume while the
/// business software is running.
/// </summary>
public sealed class BackupServiceV3 : BackupServiceBase, IDisposable
{
    private static readonly TimeSpan BusinessSoftwarePollInterval = TimeSpan.FromMilliseconds(500);

    private readonly AppSettings _appSettings;
    private readonly BusinessSoftwareMonitor _monitor;
    private readonly Timer _businessSoftwareTimer;

    /// <summary>A job currently running, with its controller and the logger of its run.</summary>
    private sealed record RunningJob(BackupJob Job, JobController Controller, ILogger Logger)
    {
        public string Name => Job.Name;
    }

    private readonly Dictionary<string, RunningJob> _running = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _runningLock = new();
    private bool _businessSoftwareRunning;
    private int _pollInProgress;

    /// <summary>Initializes the service and starts watching the business software.</summary>
    public BackupServiceV3(StateManager stateManager, AppSettings appSettings,
        BusinessSoftwareMonitor monitor, string? configPath = null)
        : base(stateManager, configPath ?? AppPaths.JobsFile("backup_config_v3.json"), maxJobs: null)
    {
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _monitor     = monitor     ?? throw new ArgumentNullException(nameof(monitor));
        _businessSoftwareTimer = new Timer(PollBusinessSoftware, null, TimeSpan.Zero, BusinessSoftwarePollInterval);
    }

    // ── Status ─────────────────────────────────────────────────────────────────

    /// <summary>Gets whether at least one job is running (or paused).</summary>
    public bool HasRunningJobs
    {
        get { lock (_runningLock) return _running.Count > 0; }
    }

    /// <summary>Gets whether the business software was running at the last check.</summary>
    public bool IsBusinessSoftwareRunning => Volatile.Read(ref _businessSoftwareRunning);

    /// <summary>Returns whether the given job is running (or paused).</summary>
    public bool IsJobRunning(string name)
    {
        lock (_runningLock) return _running.ContainsKey(name);
    }

    // ── Parallel execution ─────────────────────────────────────────────────────

    /// <summary>
    /// Launches the given jobs in parallel (jobs already running are ignored).
    /// If the business software is running, the jobs start paused and resume automatically.
    /// </summary>
    /// <returns>A task that completes when all launched jobs are finished or stopped.</returns>
    public Task ExecuteJobsParallelAsync(IEnumerable<string> names, ILogger logger)
    {
        bool pausedByBusinessSoftware = _monitor.IsRunning();
        var started = Reserve(names, logger, pausedByBusinessSoftware);

        // Every job registers its priority files before any job starts transferring
        var plans = started
            .Select(run => (Run: run, Plan: Engine.Prepare(run.Job, PriorityExtensions())))
            .ToList();

        if (pausedByBusinessSoftware)
            started.ForEach(run => BackupEngine.LogEvent(logger, run.Name, LogActions.BusinessSoftwarePaused));

        return Task.WhenAll(plans.Select(p => Task.Run(() => RunAndReleaseAsync(p.Run, p.Plan))));
    }

    private List<RunningJob> Reserve(IEnumerable<string> names, ILogger logger, bool pausedByBusinessSoftware)
    {
        var started = new List<RunningJob>();
        lock (_runningLock)
        {
            foreach (var job in names.Select(GetJob).OfType<BackupJob>())
            {
                if (_running.ContainsKey(job.Name)) continue;

                var controller = new JobController();
                controller.SetBusinessSoftwarePause(pausedByBusinessSoftware);
                var run = new RunningJob(job, controller, logger);
                _running[job.Name] = run;
                started.Add(run);
            }
        }
        return started;
    }

    private async Task RunAndReleaseAsync(RunningJob run, BackupPlan plan)
    {
        try
        {
            var options = new BackupRunOptions
            {
                EncryptionExtensions = BackupRunOptions.ToExtensionSet(_appSettings.EncryptionExtensions),
                PriorityExtensions   = PriorityExtensions(),
                LargeFileThresholdKb = _appSettings.MaxParallelFileSizeKb,
                Controller           = run.Controller
            };
            await Engine.RunAsync(plan, run.Logger, options);
        }
        finally
        {
            lock (_runningLock) _running.Remove(run.Name);
            run.Controller.Dispose();
        }
    }

    private IReadOnlySet<string> PriorityExtensions()
        => BackupRunOptions.ToExtensionSet(_appSettings.PriorityExtensions);

    // ── Play / Pause / Stop ────────────────────────────────────────────────────

    /// <summary>Pauses a job after its current file.</summary>
    public void PauseJob(string name) => ForJob(name, c => c.Pause());

    /// <summary>Resumes a job paused by the user.</summary>
    public void ResumeJob(string name) => ForJob(name, c => c.Resume());

    /// <summary>Stops a job immediately (the current transfer is cancelled).</summary>
    public void StopJob(string name) => ForJob(name, c => c.Stop());

    /// <summary>Pauses every running job after its current file.</summary>
    public void PauseAll() => ForAll(run => run.Controller.Pause());

    /// <summary>Resumes every job paused by the user.</summary>
    public void ResumeAll() => ForAll(run => run.Controller.Resume());

    /// <summary>Stops every running job immediately.</summary>
    public void StopAll() => ForAll(run => run.Controller.Stop());

    private void ForJob(string name, Action<JobController> action)
    {
        lock (_runningLock)
            if (_running.TryGetValue(name, out var run)) action(run.Controller);
    }

    private void ForAll(Action<RunningJob> action)
    {
        lock (_runningLock)
            foreach (var run in _running.Values) action(run);
    }

    // ── Business software: automatic pause / resume ────────────────────────────

    private void PollBusinessSoftware(object? _)
    {
        if (Interlocked.Exchange(ref _pollInProgress, 1) == 1) return; // Previous check still running
        try
        {
            bool isRunning = _monitor.IsRunning();
            bool changed = isRunning != _businessSoftwareRunning;
            Volatile.Write(ref _businessSoftwareRunning, isRunning);

            // Applied on every tick (not only on change) so that no job can escape the pause
            ForAll(run =>
            {
                run.Controller.SetBusinessSoftwarePause(isRunning);
                if (changed)
                    BackupEngine.LogEvent(run.Logger, run.Name,
                        isRunning ? LogActions.BusinessSoftwarePaused : LogActions.BusinessSoftwareResumed);
            });
        }
        finally
        {
            Volatile.Write(ref _pollInProgress, 0);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _businessSoftwareTimer.Dispose();
        StopAll();
    }
}

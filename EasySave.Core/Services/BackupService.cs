using EasySave.Core.Interfaces;
using EasyLog;

namespace EasySave.Core.Services;

/// <summary>
/// v1.x backup service: at most 5 jobs, executed one at a time (mono or sequential).
/// Thread-safe.
/// </summary>
public sealed class BackupService : BackupServiceBase, IBackupService
{
    /// <summary>The maximum number of jobs in v1.x.</summary>
    public const int MaxJobs = 5;

    /// <summary>
    /// Initializes a new <see cref="BackupService"/> and loads persisted job configuration.
    /// </summary>
    /// <param name="stateManager">The state manager used to persist real-time job states.</param>
    /// <param name="configPath">Optional custom config path for testing.</param>
    public BackupService(StateManager stateManager, string? configPath = null)
        : base(stateManager, configPath ?? AppPaths.JobsFile("backup_config.json"), MaxJobs)
    {
    }

    /// <inheritdoc/>
    public void ExecuteJob(string name, ILogger logger)
    {
        var job = GetJob(name);
        if (job is not null)
            RunSequentially(job, logger, BackupRunOptions.Default);
    }

    /// <inheritdoc/>
    public void ExecuteJobs(IEnumerable<string> names, ILogger logger)
    {
        foreach (var name in names)
            ExecuteJob(name, logger);
    }
}

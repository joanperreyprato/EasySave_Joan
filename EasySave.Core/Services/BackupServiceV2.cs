using EasySave.Core.Interfaces;
using EasyLog;

namespace EasySave.Core.Services;

/// <summary>
/// v2.0 backup service: unlimited jobs, CryptoSoft encryption and business software detection.
/// A job cannot start while the business software runs; if it starts during a sequential run,
/// the file in progress is finished, the stop is logged and the remaining files and jobs are skipped.
/// </summary>
public sealed class BackupServiceV2 : BackupServiceBase, IBackupService
{
    private readonly AppSettings _appSettings;
    private readonly BusinessSoftwareMonitor _monitor;

    /// <summary>
    /// Initializes a new <see cref="BackupServiceV2"/> with all v2.0 dependencies.
    /// </summary>
    public BackupServiceV2(StateManager stateManager, AppSettings appSettings,
        BusinessSoftwareMonitor monitor, string? configPath = null)
        : base(stateManager, configPath ?? AppPaths.JobsFile("backup_config_v2.json"), maxJobs: null)
    {
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _monitor     = monitor     ?? throw new ArgumentNullException(nameof(monitor));
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">The business software is (or started) running.</exception>
    public void ExecuteJob(string name, ILogger logger)
    {
        var job = GetJob(name);
        if (job is null) return;

        if (_monitor.IsRunning())
        {
            BackupEngine.LogEvent(logger, name, LogActions.BusinessSoftwareBlocked);
            throw BusinessSoftwareException("blocked");
        }

        var options = new BackupRunOptions
        {
            EncryptionExtensions = BackupRunOptions.ToExtensionSet(_appSettings.EncryptionExtensions),
            InterruptRequested   = _monitor.IsRunning
        };

        if (RunSequentially(job, logger, options) == BackupOutcome.Interrupted)
            throw BusinessSoftwareException("stopped");
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">The business software is (or started) running.</exception>
    public void ExecuteJobs(IEnumerable<string> names, ILogger logger)
    {
        foreach (var name in names)
            ExecuteJob(name, logger);
    }

    private InvalidOperationException BusinessSoftwareException(string verb)
        => new($"Backup {verb}: business software '{_appSettings.BusinessSoftwareName}' is running.");
}

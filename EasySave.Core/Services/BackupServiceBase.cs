using EasySave.Core.Models;
using EasyLog;

namespace EasySave.Core.Services;

/// <summary>
/// Behavior shared by every version of the backup service: job management (through a
/// <see cref="JobCatalog"/>) and backup execution (through a <see cref="BackupEngine"/>).
/// Each version only adds its own rules (job limit, encryption, business software, parallelism).
/// </summary>
public abstract class BackupServiceBase
{
    private readonly JobCatalog _catalog;

    /// <summary>Initializes the shared services.</summary>
    /// <param name="stateManager">Writes the real-time state file.</param>
    /// <param name="configPath">The JSON file holding the jobs.</param>
    /// <param name="maxJobs">The maximum number of jobs, or <c>null</c> for unlimited.</param>
    protected BackupServiceBase(StateManager stateManager, string configPath, int? maxJobs)
    {
        StateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        Engine = new BackupEngine(stateManager);
        _catalog = new JobCatalog(configPath, maxJobs);
    }

    /// <summary>Gets the real-time state writer.</summary>
    protected StateManager StateManager { get; }

    /// <summary>Gets the backup engine.</summary>
    protected BackupEngine Engine { get; }

    /// <summary>Creates a new backup job after validating all parameters.</summary>
    public BackupCreationResult CreateJob(string name, string sourceDirectory, string targetDirectory, BackupType type)
        => _catalog.Add(name, sourceDirectory, targetDirectory, type);

    /// <summary>Deletes the backup job with the given name and its state.</summary>
    public void DeleteJob(string name)
    {
        if (_catalog.Remove(name))
            StateManager.RemoveJobState(name);
    }

    /// <summary>Returns all configured backup jobs in creation order.</summary>
    public List<BackupJob> GetAllJobs() => _catalog.GetAll();

    /// <summary>Returns the job with the given name, or <c>null</c> if not found.</summary>
    public BackupJob? GetJob(string name) => _catalog.Find(name);

    /// <summary>Runs one job to completion on the calling thread (v1.x and v2.0 sequential mode).</summary>
    protected BackupOutcome RunSequentially(BackupJob job, ILogger logger, BackupRunOptions options)
    {
        var plan = Engine.Prepare(job, options.PriorityExtensions);
        return Engine.RunAsync(plan, logger, options).GetAwaiter().GetResult();
    }
}

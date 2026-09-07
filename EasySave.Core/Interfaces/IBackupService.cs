using EasySave.Core.Models;
using EasyLog;

namespace EasySave.Core.Interfaces;

/// <summary>
/// Defines the operations available on the backup job manager.
/// </summary>
public interface IBackupService
{
    /// <summary>Creates a new backup job after validating all parameters.</summary>
    /// <param name="name">Unique job name (1–50 alphanumeric/dash/underscore characters).</param>
    /// <param name="sourceDirectory">Existing source directory path.</param>
    /// <param name="targetDirectory">Target directory path (parent must exist).</param>
    /// <param name="type">Full or Differential backup.</param>
    /// <returns>A <see cref="BackupCreationResult"/> indicating success or the failure reason.</returns>
    BackupCreationResult CreateJob(string name, string sourceDirectory, string targetDirectory, BackupType type);

    /// <summary>Deletes the backup job with the given name.</summary>
    /// <param name="name">The name of the job to delete.</param>
    void DeleteJob(string name);

    /// <summary>Returns all configured backup jobs in creation order.</summary>
    List<BackupJob> GetAllJobs();

    /// <summary>Returns the job with the given name, or <c>null</c> if not found.</summary>
    /// <param name="name">The job name to look up.</param>
    BackupJob? GetJob(string name);

    /// <summary>Executes a single backup job by name.</summary>
    /// <param name="name">The name of the job to execute.</param>
    /// <param name="logger">The logger to record file transfer events.</param>
    void ExecuteJob(string name, ILogger logger);

    /// <summary>Executes multiple backup jobs sequentially.</summary>
    /// <param name="names">The names of the jobs to execute, in order.</param>
    /// <param name="logger">The logger to record file transfer events.</param>
    void ExecuteJobs(IEnumerable<string> names, ILogger logger);
}

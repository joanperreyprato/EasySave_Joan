namespace EasySave.Core.Models;

/// <summary>Represents the possible lifecycle states of a backup job.</summary>
public enum JobStatus
{
    /// <summary>The job has not been started.</summary>
    Inactive,
    /// <summary>The job is currently running.</summary>
    Active,
    /// <summary>The job has been paused by the user.</summary>
    Paused,
    /// <summary>The job finished successfully.</summary>
    Completed,
    /// <summary>The job stopped due to an error.</summary>
    Error
}

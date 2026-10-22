namespace EasySave.Core.Models;

/// <summary>Represents the possible lifecycle states of a backup job.</summary>
public enum JobStatus
{
    /// <summary>The job has not been started.</summary>
    Inactive,
    /// <summary>The job is currently running.</summary>
    Active,
    /// <summary>The job is paused (by the user or because the business software is running).</summary>
    Paused,
    /// <summary>The job finished successfully.</summary>
    Completed,
    /// <summary>The job stopped due to an error.</summary>
    Error,
    /// <summary>The job was stopped by the user or interrupted by the business software.</summary>
    Stopped
}

/// <summary>Helpers shared by the user interfaces to display a <see cref="JobStatus"/>.</summary>
public static class JobStatusExtensions
{
    /// <summary>Returns the translation key of the status (e.g. "status_paused").</summary>
    public static string TranslationKey(this JobStatus status) => status switch
    {
        JobStatus.Active    => "status_active",
        JobStatus.Paused    => "status_paused",
        JobStatus.Completed => "status_completed",
        JobStatus.Error     => "status_error",
        JobStatus.Stopped   => "status_stopped",
        _                   => "status_inactive"
    };
}

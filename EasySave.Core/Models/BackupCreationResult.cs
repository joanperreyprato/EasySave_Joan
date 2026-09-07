namespace EasySave.Core.Models;

/// <summary>Enumerates the possible outcomes when creating a backup job.</summary>
public enum BackupCreationResult
{
    /// <summary>The job was created successfully.</summary>
    Success,
    /// <summary>The maximum number of allowed jobs (5) has been reached.</summary>
    MaxJobsExceeded,
    /// <summary>The provided job name is invalid (empty, too long, or contains illegal characters).</summary>
    InvalidName,
    /// <summary>The source directory does not exist.</summary>
    SourceNotFound,
    /// <summary>The parent of the target directory does not exist.</summary>
    TargetNotFound,
    /// <summary>A job with the same name already exists.</summary>
    DuplicateName
}

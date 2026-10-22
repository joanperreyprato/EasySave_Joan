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
    DuplicateName,
    /// <summary>The source or the target is not a full path (e.g. "C:\Data" or "\\server\share").</summary>
    PathNotFullyQualified,
    /// <summary>The target is the source directory itself or one of its sub-directories.</summary>
    TargetInsideSource
}

/// <summary>Helpers shared by the user interfaces to display a <see cref="BackupCreationResult"/>.</summary>
public static class BackupCreationResultExtensions
{
    /// <summary>Returns the translation key of the result message (e.g. "error_source_not_found").</summary>
    public static string TranslationKey(this BackupCreationResult result) => result switch
    {
        BackupCreationResult.Success               => "success_job_created",
        BackupCreationResult.MaxJobsExceeded       => "error_max_jobs",
        BackupCreationResult.InvalidName           => "error_invalid_name",
        BackupCreationResult.SourceNotFound        => "error_source_not_found",
        BackupCreationResult.TargetNotFound        => "error_target_not_found",
        BackupCreationResult.DuplicateName         => "error_duplicate_name",
        BackupCreationResult.PathNotFullyQualified => "error_path_not_full",
        BackupCreationResult.TargetInsideSource    => "error_target_inside_source",
        _                                          => "error_unknown"
    };
}

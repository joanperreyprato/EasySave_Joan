namespace EasyLog;

/// <summary>Well-known values for <see cref="LogEntry.Action"/>.</summary>
public static class LogActions
{
    /// <summary>A file was copied from the source to the target directory.</summary>
    public const string FileTransfer = "FileTransfer";

    /// <summary>A directory was created in the target tree.</summary>
    public const string DirectoryCreated = "DirectoryCreated";

    /// <summary>A job could not start because the business software is running.</summary>
    public const string BusinessSoftwareBlocked = "BusinessSoftwareBlocked";

    /// <summary>A sequential job stopped after the current file because the business software started (v2.0).</summary>
    public const string BusinessSoftwareInterrupted = "BusinessSoftwareInterrupted";

    /// <summary>A job was paused because the business software is running (v3.0).</summary>
    public const string BusinessSoftwarePaused = "BusinessSoftwarePaused";

    /// <summary>A job resumed because the business software was closed.</summary>
    public const string BusinessSoftwareResumed = "BusinessSoftwareResumed";

    /// <summary>A job was stopped by the user.</summary>
    public const string JobStopped = "JobStopped";
}

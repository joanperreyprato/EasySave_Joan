namespace EasySave.Core.Models;

/// <summary>
/// Represents the real-time state of a backup job, persisted to state.json.
/// </summary>
public sealed class JobState
{
    /// <summary>Gets or sets the name of the backup job.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp of the last recorded action.</summary>
    public DateTime LastActionTime { get; set; } = DateTime.Now;

    /// <summary>Gets or sets the current execution status of the job.</summary>
    public JobStatus Status { get; set; } = JobStatus.Inactive;

    /// <summary>Gets or sets the total number of eligible files for this backup.</summary>
    public int TotalFiles { get; set; }

    /// <summary>Gets or sets the total size (in bytes) of all eligible files.</summary>
    public long TotalSize { get; set; }

    /// <summary>Gets or sets the number of files not yet transferred.</summary>
    public int RemainingFiles { get; set; }

    /// <summary>Gets or sets the total size (in bytes) of remaining files.</summary>
    public long RemainingSize { get; set; }

    /// <summary>Gets or sets the completion percentage (0.0 to 100.0).</summary>
    public double Progress { get; set; }

    /// <summary>Gets or sets the full UNC path of the file currently being copied.</summary>
    public string CurrentSourceFile { get; set; } = string.Empty;

    /// <summary>Gets or sets the full UNC path of the current destination file.</summary>
    public string CurrentTargetFile { get; set; } = string.Empty;
}

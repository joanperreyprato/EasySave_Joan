using EasySave.Core.Models;

namespace EasySave.Core.Services;

/// <summary>A file selected for backup, with its size and priority computed once.</summary>
/// <param name="Path">Full path of the source file.</param>
/// <param name="Size">Size in bytes when the plan was built.</param>
/// <param name="IsPriority">Whether the file has a priority extension.</param>
public sealed record PlannedFile(string Path, long Size, bool IsPriority);

/// <summary>
/// The list of files a job will transfer, priority files first.
/// Building a plan registers its priority files in the <see cref="PriorityTransferGuard"/>.
/// </summary>
/// <param name="Job">The job being run.</param>
/// <param name="Files">The eligible files, priority files first.</param>
/// <param name="Error">The reason why the source could not be read, or <c>null</c>.</param>
public sealed record BackupPlan(BackupJob Job, IReadOnlyList<PlannedFile> Files, string? Error)
{
    /// <summary>Gets the number of priority files in the plan.</summary>
    public int PriorityFileCount => Files.Count(f => f.IsPriority);

    /// <summary>Gets the total size in bytes of the files to transfer.</summary>
    public long TotalSize => Files.Sum(f => f.Size);
}

/// <summary>How a backup run ended.</summary>
public enum BackupOutcome
{
    /// <summary>Every file was processed.</summary>
    Completed,
    /// <summary>The user stopped the job.</summary>
    Stopped,
    /// <summary>The job stopped after the current file because the business software started (v2.0).</summary>
    Interrupted,
    /// <summary>The source directory could not be read.</summary>
    Failed
}

namespace EasySave.Core.Services;

/// <summary>
/// Thread-safe guard shared by all jobs that enforces the priority-file rule (v3.0):
/// no non-priority file may be transferred while at least one job still has
/// priority files waiting to be transferred.
/// </summary>
/// <remarks>
/// Each job registers all of its priority files before any job starts, releases them one by one
/// as they are transferred, and releases the remaining ones when it ends (completed, stopped or failed).
/// </remarks>
public sealed class PriorityTransferGuard
{
    private const int PollDelayMs = 50;
    private int _pendingPriorityFiles;

    /// <summary>Gets the instance shared by every job of the application.</summary>
    public static PriorityTransferGuard Instance { get; } = new();

    /// <summary>Gets the number of priority files still waiting in all jobs.</summary>
    public int PendingCount => Volatile.Read(ref _pendingPriorityFiles);

    /// <summary>Declares priority files that a job is going to transfer.</summary>
    /// <param name="count">The number of priority files in the job.</param>
    public void Register(int count)
    {
        if (count > 0) Interlocked.Add(ref _pendingPriorityFiles, count);
    }

    /// <summary>Removes priority files from the pending count (transferred or abandoned).</summary>
    /// <param name="count">The number of priority files to release.</param>
    public void Release(int count = 1)
    {
        if (count > 0) Interlocked.Add(ref _pendingPriorityFiles, -count);
    }

    /// <summary>Waits until no priority file is pending in any job.</summary>
    /// <param name="ct">Cancels the wait when the job is stopped.</param>
    public async Task WaitUntilNoPriorityPendingAsync(CancellationToken ct)
    {
        while (PendingCount > 0)
            await Task.Delay(PollDelayMs, ct);
    }
}

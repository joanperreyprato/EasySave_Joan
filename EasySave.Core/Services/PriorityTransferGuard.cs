namespace EasySave.Core.Services;

/// <summary>
/// Thread-safe shared guard that enforces the priority-file rule (v3.0).
/// No non-priority file may be transferred while any job has a priority file waiting.
/// </summary>
public sealed class PriorityTransferGuard
{
    private static readonly PriorityTransferGuard _instance = new();
    public static PriorityTransferGuard Instance => _instance;

    private int _priorityFilesWaiting;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// Called before transferring a file. Blocks until it is safe to proceed.
    /// Priority files are always allowed; non-priority files wait if any priority file is pending.
    /// </summary>
    public async Task AcquireAsync(bool isPriority, CancellationToken ct)
    {
        if (isPriority)
        {
            Interlocked.Increment(ref _priorityFilesWaiting);
            return; // Priority files never wait
        }

        // Non-priority: spin-wait until no priority files are pending
        while (Volatile.Read(ref _priorityFilesWaiting) > 0)
            await Task.Delay(50, ct);
    }

    /// <summary>Called after a priority file transfer completes.</summary>
    public void ReleasePriority() => Interlocked.Decrement(ref _priorityFilesWaiting);
}

namespace EasySave.Core.Services;

/// <summary>
/// Thread-safe shared guard that prevents parallel transfer of large files (v3.0).
/// Only one file larger than the configured threshold can be transferred at a time.
/// Small files can always be transferred in parallel (subject to priority rules).
/// </summary>
public sealed class LargeFileTransferGuard
{
    private static readonly LargeFileTransferGuard _instance = new();
    public static LargeFileTransferGuard Instance => _instance;

    private readonly SemaphoreSlim _largeSemaphore = new(1, 1);

    /// <summary>
    /// Acquires the large-file slot if the file exceeds the threshold.
    /// Returns an IDisposable that releases the slot on dispose.
    /// Small files get a no-op disposable immediately.
    /// </summary>
    public async Task<IDisposable> AcquireAsync(long fileSizeBytes, long thresholdKb, CancellationToken ct)
    {
        long thresholdBytes = thresholdKb * 1024;
        if (thresholdKb <= 0 || fileSizeBytes <= thresholdBytes)
            return NullDisposable.Instance; // Small file - no restriction

        await _largeSemaphore.WaitAsync(ct);
        return new SemaphoreReleaser(_largeSemaphore);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class SemaphoreReleaser(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();
        public void Dispose() { }
    }
}

namespace EasySave.Core.Services;

/// <summary>
/// Manages the Pause / Resume / Stop lifecycle of a single backup job (v3.0).
/// Uses a ManualResetEventSlim for pause (blocks the worker thread)
/// and a CancellationTokenSource for stop.
/// </summary>
public sealed class JobController : IDisposable
{
    private readonly ManualResetEventSlim _pauseEvent = new(initialState: true); // true = not paused
    private readonly CancellationTokenSource _cts = new();

    /// <summary>Gets the cancellation token for this job. Pass it to async operations.</summary>
    public CancellationToken Token => _cts.Token;

    /// <summary>Suspends the job after the current file finishes.</summary>
    public void Pause() => _pauseEvent.Reset();

    /// <summary>Resumes a paused job.</summary>
    public void Resume() => _pauseEvent.Set();

    /// <summary>Requests cancellation of the job.</summary>
    public void Stop() => _cts.Cancel();

    /// <summary>
    /// Called by the worker thread between file transfers.
    /// Blocks if paused; throws OperationCanceledException if stopped.
    /// </summary>
    public void CheckPauseOrStop()
    {
        _cts.Token.ThrowIfCancellationRequested();
        _pauseEvent.Wait(_cts.Token); // Blocks while paused, throws if cancelled
    }

    public void Dispose()
    {
        _pauseEvent.Dispose();
        _cts.Dispose();
    }
}

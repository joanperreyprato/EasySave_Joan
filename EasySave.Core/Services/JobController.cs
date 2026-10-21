namespace EasySave.Core.Services;

/// <summary>
/// Manages the Play / Pause / Stop lifecycle of a single running backup job (v3.0).
/// A job can be paused for two independent reasons: by the user, or because the business
/// software is running. It only runs again once both reasons are cleared, so closing the
/// business software never resumes a job the user paused (and vice versa).
/// </summary>
public sealed class JobController : IDisposable
{
    private readonly ManualResetEventSlim _runEvent = new(initialState: true); // set = may run
    private readonly CancellationTokenSource _cts = new();
    private readonly object _lock = new();
    private bool _pausedByUser;
    private bool _pausedByBusinessSoftware;

    /// <summary>Gets the cancellation token for this job. Pass it to async operations.</summary>
    public CancellationToken Token => _cts.Token;

    /// <summary>Gets whether the job is currently paused (for any reason).</summary>
    public bool IsPaused
    {
        get { lock (_lock) return _pausedByUser || _pausedByBusinessSoftware; }
    }

    /// <summary>Pauses the job after the current file finishes (user request).</summary>
    public void Pause() => Update(byUser: true, byBusinessSoftware: null);

    /// <summary>Resumes a job paused by the user (user request).</summary>
    public void Resume() => Update(byUser: false, byBusinessSoftware: null);

    /// <summary>Pauses or resumes the job depending on whether the business software is running.</summary>
    public void SetBusinessSoftwarePause(bool paused) => Update(byUser: null, byBusinessSoftware: paused);

    /// <summary>Requests the immediate stop of the job (the current transfer is cancelled).</summary>
    public void Stop() => _cts.Cancel();

    /// <summary>
    /// Called by the worker between file transfers.
    /// Blocks while paused; throws <see cref="OperationCanceledException"/> if stopped.
    /// </summary>
    public void WaitWhilePaused()
    {
        _cts.Token.ThrowIfCancellationRequested();
        _runEvent.Wait(_cts.Token);
    }

    private void Update(bool? byUser, bool? byBusinessSoftware)
    {
        lock (_lock)
        {
            _pausedByUser = byUser ?? _pausedByUser;
            _pausedByBusinessSoftware = byBusinessSoftware ?? _pausedByBusinessSoftware;

            if (_pausedByUser || _pausedByBusinessSoftware) _runEvent.Reset();
            else _runEvent.Set();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _runEvent.Dispose();
        _cts.Dispose();
    }
}

using System.Collections.Concurrent;
using System.Threading;

namespace EasySave.Core
{
    /// <summary>
    /// Manages execution control (pause, stop, resume) for backup jobs
    /// </summary>
    public class JobController
    {
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens = new();
        private readonly ConcurrentDictionary<string, ManualResetEventSlim> _pauseEvents = new();
        private readonly object _lock = new object();

        /// <summary>
        /// Registers a new job for control
        /// </summary>
        public CancellationToken RegisterJob(string jobName)
        {
            lock (_lock)
            {
                // Clean up existing tokens if any
                if (_cancellationTokens.TryRemove(jobName, out var oldCts))
                {
                    try { oldCts.Dispose(); } catch { }
                }

                if (_pauseEvents.TryRemove(jobName, out var oldEvent))
                {
                    try { oldEvent.Dispose(); } catch { }
                }

                // Create new control structures
                var cts = new CancellationTokenSource();
                var pauseEvent = new ManualResetEventSlim(true); // Start in signaled state (not paused)

                _cancellationTokens[jobName] = cts;
                _pauseEvents[jobName] = pauseEvent;

                return cts.Token;
            }
        }

        /// <summary>
        /// Pauses a job (will take effect after current file finishes)
        /// </summary>
        public void PauseJob(string jobName)
        {
            if (_pauseEvents.TryGetValue(jobName, out var pauseEvent))
            {
                pauseEvent.Reset(); // Set to non-signaled state
            }
        }

        /// <summary>
        /// Resumes a paused job
        /// </summary>
        public void ResumeJob(string jobName)
        {
            if (_pauseEvents.TryGetValue(jobName, out var pauseEvent))
            {
                pauseEvent.Set(); // Set to signaled state
            }
        }

        /// <summary>
        /// Stops a job immediately
        /// </summary>
        public void StopJob(string jobName)
        {
            if (_cancellationTokens.TryGetValue(jobName, out var cts))
            {
                try
                {
                    cts.Cancel();
                }
                catch { }
            }

            // Also resume if paused, so the cancellation can be detected
            ResumeJob(jobName);
        }

        /// <summary>
        /// Checks if the job should pause and waits if needed. Returns false if job was cancelled.
        /// </summary>
        public bool CheckPauseOrCancel(string jobName)
        {
            // Check for cancellation first
            if (_cancellationTokens.TryGetValue(jobName, out var cts))
            {
                if (cts.Token.IsCancellationRequested)
                {
                    return false; // Job was stopped
                }
            }

            // Check for pause
            if (_pauseEvents.TryGetValue(jobName, out var pauseEvent))
            {
                pauseEvent.Wait(); // Blocks if paused until resumed
            }

            // Check cancellation again after waiting
            if (_cancellationTokens.TryGetValue(jobName, out var cts2))
            {
                if (cts2.Token.IsCancellationRequested)
                {
                    return false; // Job was stopped while paused
                }
            }

            return true; // Continue
        }

        /// <summary>
        /// Checks if a job is currently paused
        /// </summary>
        public bool IsPaused(string jobName)
        {
            if (_pauseEvents.TryGetValue(jobName, out var pauseEvent))
            {
                return !pauseEvent.IsSet;
            }
            return false;
        }

        /// <summary>
        /// Checks if a job has been cancelled
        /// </summary>
        public bool IsCancelled(string jobName)
        {
            if (_cancellationTokens.TryGetValue(jobName, out var cts))
            {
                return cts.Token.IsCancellationRequested;
            }
            return false;
        }

        /// <summary>
        /// Unregisters a job and cleans up resources
        /// </summary>
        public void UnregisterJob(string jobName)
        {
            lock (_lock)
            {
                if (_cancellationTokens.TryRemove(jobName, out var cts))
                {
                    try { cts.Dispose(); } catch { }
                }

                if (_pauseEvents.TryRemove(jobName, out var pauseEvent))
                {
                    try { pauseEvent.Dispose(); } catch { }
                }
            }
        }

        /// <summary>
        /// Clears all registered jobs
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                foreach (var kvp in _cancellationTokens)
                {
                    try { kvp.Value.Dispose(); } catch { }
                }
                _cancellationTokens.Clear();

                foreach (var kvp in _pauseEvents)
                {
                    try { kvp.Value.Dispose(); } catch { }
                }
                _pauseEvents.Clear();
            }
        }
    }
}

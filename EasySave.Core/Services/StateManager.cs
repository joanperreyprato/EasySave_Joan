using System.Text.Json;
using System.Text.Json.Serialization;
using EasySave.Core.Models;

namespace EasySave.Core.Services;

/// <summary>
/// Persists real-time job states to a single JSON file (<c>state.json</c>).
/// Thread-safe. All operations are atomic with respect to the state file.
/// </summary>
public sealed class StateManager
{
    private readonly string _statePath;
    // Statuses are written as text ("Active", "Paused"...) so that state.json stays readable in Notepad
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new <see cref="StateManager"/> and ensures the storage directory exists.
    /// </summary>
    public StateManager(string? statePath = null)
    {
        _statePath = statePath ?? AppPaths.StateFile;
        Directory.CreateDirectory(Path.GetDirectoryName(_statePath)!);
    }

    /// <summary>
    /// Updates or inserts the state for the given job, then persists to disk.
    /// </summary>
    /// <param name="state">The current state of the job.</param>
    public void UpdateJobState(JobState state)
    {
        lock (_lock)
        {
            var states = LoadInternal();
            int idx = states.FindIndex(s => s.Name == state.Name);
            if (idx >= 0) states[idx] = state;
            else states.Add(state);
            SaveInternal(states);
        }
    }

    /// <summary>
    /// Removes the state entry for the job with the given name.
    /// </summary>
    /// <param name="jobName">The name of the job whose state should be removed.</param>
    public void RemoveJobState(string jobName)
    {
        lock (_lock)
        {
            var states = LoadInternal();
            states.RemoveAll(s => s.Name == jobName);
            SaveInternal(states);
        }
    }

    /// <summary>
    /// Returns all currently persisted job states.
    /// </summary>
    /// <returns>A list of <see cref="JobState"/> objects, possibly empty.</returns>
    public List<JobState> GetAllStates()
    {
        lock (_lock) return LoadInternal();
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private List<JobState> LoadInternal()
    {
        if (!File.Exists(_statePath)) return [];
        try
        {
            var json = File.ReadAllText(_statePath);
            return JsonSerializer.Deserialize<List<JobState>>(json, JsonOptions) ?? [];
        }
        catch { return []; }
    }

    private void SaveInternal(List<JobState> states)
    {
        var json = JsonSerializer.Serialize(states, JsonOptions);
        File.WriteAllText(_statePath, json);
    }
}

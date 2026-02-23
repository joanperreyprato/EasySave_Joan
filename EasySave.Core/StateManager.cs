using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EasySave.Core
{
    public class StateManager
    {
        private readonly string stateFilePath;
        private List<JobState> jobStates;
        // Lock object for thread-safe access to jobStates in parallel execution
        private readonly object _lockObject = new object();

        private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public StateManager(string? stateFilePath = null)
        {
            if (string.IsNullOrWhiteSpace(stateFilePath))
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string dir = Path.Combine(appData, "EasySave");
                Directory.CreateDirectory(dir);
                this.stateFilePath = Path.Combine(dir, "state.json");
            }
            else
            {
                this.stateFilePath = stateFilePath;
            }

            jobStates = new List<JobState>();
        }

        // Update or add a job state by name (thread-safe for parallel execution)
        public void UpdateJobState(string jobName, JobState state)
        {
            if (string.IsNullOrWhiteSpace(jobName) || state == null)
                return;

            lock (_lockObject)
            {
                JobState? existing = jobStates.FirstOrDefault(j => string.Equals(j.Name, jobName, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    // replace properties
                    existing.Timestamp = state.Timestamp;
                    existing.Status = state.Status;
                    existing.TotalFiles = state.TotalFiles;
                    existing.TotalSize = state.TotalSize;
                    existing.RemainingFiles = state.RemainingFiles;
                    existing.RemainingSize = state.RemainingSize;
                    existing.CurrentSourceFile = state.CurrentSourceFile;
                    existing.CurrentTargetFile = state.CurrentTargetFile;
                    existing.Progress = state.Progress;
                }
                else
                {
                    jobStates.Add(state);
                }
            }
        }

        // Retrieve a job state by name (or null if not found) - thread-safe
        public JobState? GetJobState(string jobName)
        {
            if (string.IsNullOrWhiteSpace(jobName)) return null;

            lock (_lockObject)
            {
                return jobStates.FirstOrDefault(j => string.Equals(j.Name, jobName, StringComparison.OrdinalIgnoreCase));
            }
        }

        // Persist current states to disk (thread-safe)
        public void SaveState()
        {
            try
            {
                string json;
                lock (_lockObject)
                {
                    json = SerializeToJson();
                }
                File.WriteAllText(stateFilePath, json);
            }
            catch
            {
                // ignore errors for now
            }
        }

        // Load states from disk (if file exists) - thread-safe
        public void LoadState()
        {
            try
            {
                if (!File.Exists(stateFilePath))
                {
                    lock (_lockObject)
                    {
                        jobStates = new List<JobState>();
                    }
                    return;
                }

                string json = File.ReadAllText(stateFilePath);
                List<JobState>? list = JsonSerializer.Deserialize<List<JobState>>(json, CachedJsonSerializerOptions);

                lock (_lockObject)
                {
                    jobStates = list ?? new List<JobState>();
                }
            }
            catch
            {
                lock (_lockObject)
                {
                    jobStates = new List<JobState>();
                }
            }
        }

        // Return JSON representation of the current states
        private string SerializeToJson()
        {
            return JsonSerializer.Serialize(jobStates, CachedJsonSerializerOptions);
        }
    }
}

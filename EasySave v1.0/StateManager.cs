using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EasySave_v1._0
{
    public class StateManager
    {
        private readonly string stateFilePath;
        private List<JobState> jobStates;

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

        // Update or add a job state by name
        public void UpdateJobState(string jobName, JobState state)
        {
            if (string.IsNullOrWhiteSpace(jobName) || state == null)
                return;

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

        // Retrieve a job state by name (or null if not found)
        public JobState? GetJobState(string jobName)
        {
            if (string.IsNullOrWhiteSpace(jobName)) return null;
            return jobStates.FirstOrDefault(j => string.Equals(j.Name, jobName, StringComparison.OrdinalIgnoreCase));
        }

        // Persist current states to disk
        public void SaveState()
        {
            try
            {
                string json = SerializeToJson();
                File.WriteAllText(stateFilePath, json);
            }
            catch
            {
                // ignore errors for now
            }
        }

        // Load states from disk (if file exists)
        public void LoadState()
        {
            try
            {
                if (!File.Exists(stateFilePath))
                {
                    jobStates = new List<JobState>();
                    return;
                }

                string json = File.ReadAllText(stateFilePath);
                List<JobState>? list = JsonSerializer.Deserialize<List<JobState>>(json, CachedJsonSerializerOptions);
                jobStates = list ?? new List<JobState>();
            }
            catch
            {
                jobStates = new List<JobState>();
            }
        }

        // Return JSON representation of the current states
        private string SerializeToJson()
        {
            return JsonSerializer.Serialize(jobStates, CachedJsonSerializerOptions);
        }
    }
}

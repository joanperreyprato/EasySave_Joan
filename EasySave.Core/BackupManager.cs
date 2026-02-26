using EasyLog;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EasySave.Core
{
    public class BackupManager
    {
        // Threshold in KB: files larger than this are considered "large". 0 disables the restriction.
        private readonly int _largeFileThresholdKb;

        private readonly List<BackupJob> backupJobs;
        private readonly StateManager stateManager;
        private readonly ILogger logger;
        private readonly BusinessSoftwareMonitor businessMonitor;
        private readonly List<string> priorityExtensions;
        private readonly JobController jobController;

        private readonly string configPath;

        // Cached JsonSerializerOptions instance
        private static readonly JsonSerializerOptions CachedJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        // Validation error types
        public enum BackupCreationError
        {
            None,
            InvalidName,
            SourcePathInvalid,
            SourcePathNotFound,
            TargetPathInvalid,
            TargetPathNotFound,
            DuplicateName,
            MaxJobsExceeded
        }

        public BackupManager(StateManager stateManager, ILogger logger, string? configPath = null, int largeFileThresholdKb = 0)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.logger = logger;
            _largeFileThresholdKb = largeFileThresholdKb;
            this.backupJobs = new List<BackupJob>();
            this.businessMonitor = new BusinessSoftwareMonitor();
            this.priorityExtensions = new List<string>();
            this.jobController = new JobController();

            if (string.IsNullOrWhiteSpace(configPath))
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string dir = Path.Combine(appData, "EasySave");
                Directory.CreateDirectory(dir);
                this.configPath = Path.Combine(dir, "backup_config.json");
            }
            else
            {
                this.configPath = configPath;
            }

            LoadConfiguration();
        }

        // Expose config path for diagnostics
        public string ConfigPath => configPath;

        // Expose JobController for external control
        public JobController JobController => jobController;

        /// <summary>
        /// Validates the backup job name.
        /// </summary>
        private bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Only alphanumeric characters, spaces, hyphens, and underscores allowed
            var validNamePattern = @"^[a-zA-Z0-9\s\-_]+$";
            if (!Regex.IsMatch(name, validNamePattern))
                return false;

            // Maximum 50 characters
            if (name.Length > 50)
                return false;

            return true;
        }

        /// <summary>
        /// Validates a directory path.
        /// </summary>
        private bool IsValidPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                // Verify that the path is absolute and valid
                var fullPath = Path.GetFullPath(path);
                return !string.IsNullOrEmpty(fullPath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a backup job with full validation.
        /// </summary>
        public bool CreateBackupJob(string name, string source, string target, BackupType type, out BackupCreationError error)
        {
            return CreateBackupJob(name, source, target, type, EasyLog.LogFormat.Json, out error);
        }

        public bool CreateBackupJob(string name, string source, string target, BackupType type, EasyLog.LogFormat logFormat, out BackupCreationError error)
        {
            error = BackupCreationError.None;

            // Validate job name
            if (!IsValidName(name))
            {
                error = BackupCreationError.InvalidName;
                return false;
            }

            // Check for duplicate names
            if (backupJobs.Any(b => string.Equals(b.GetName(), name, StringComparison.OrdinalIgnoreCase)))
            {
                error = BackupCreationError.DuplicateName;
                return false;
            }

            // Limit to 5 jobs
            if (backupJobs.Count >= 5)
            {
                error = BackupCreationError.MaxJobsExceeded;
                return false;
            }

            // Validate source path
            if (!IsValidPath(source))
            {
                error = BackupCreationError.SourcePathInvalid;
                return false;
            }

            // Verify source directory exists
            if (!Directory.Exists(source))
            {
                error = BackupCreationError.SourcePathNotFound;
                return false;
            }

            // Validate target path
            if (!IsValidPath(target))
            {
                error = BackupCreationError.TargetPathInvalid;
                return false;
            }

            // Verify target directory exists
            if (!Directory.Exists(target))
            {
                error = BackupCreationError.TargetPathNotFound;
                return false;
            }

            BackupJob job = new BackupJob(name, source, target, type, logFormat);
            backupJobs.Add(job);
            SaveConfiguration();
            return true;
        }

        /// <summary>
        /// Creates a backup job (overload for backward compatibility).
        /// </summary>
        public bool CreateBackupJob(string name, string source, string target, BackupType type)
        {
            return CreateBackupJob(name, source, target, type, out _);
        }

        public bool DeleteBackupJob(int index)
        {
            if (index < 0 || index >= backupJobs.Count) return false;
            backupJobs.RemoveAt(index);
            SaveConfiguration();
            return true;
        }

        /// <summary>
        /// Update an existing backup job. Index is 1-based to match UI.
        /// Fields that are null or empty will keep their current values.
        /// </summary>
        public bool UpdateBackupJob(int index, string? name, string? source, string? target, BackupType type, out BackupCreationError error)
        {
            error = BackupCreationError.None;

            // Adjust for 0-based internal list
            index -= 1;
            if (index < 0 || index >= backupJobs.Count)
            {
                error = BackupCreationError.InvalidName; // reuse a generic error
                return false;
            }

            BackupJob existing = backupJobs[index];
            string newName = string.IsNullOrWhiteSpace(name) ? existing.GetName() : name!;
            string newSource = string.IsNullOrWhiteSpace(source) ? existing.GetSourcePath() : source!;
            string newTarget = string.IsNullOrWhiteSpace(target) ? existing.GetTargetPath() : target!;

            // Validate name if changed
            if (!string.Equals(newName, existing.GetName(), StringComparison.OrdinalIgnoreCase))
            {
                if (!IsValidName(newName))
                {
                    error = BackupCreationError.InvalidName;
                    return false;
                }

                // Check for duplicates (excluding this index)
                if (backupJobs.Where((b, i) => i != index).Any(b => string.Equals(b.GetName(), newName, StringComparison.OrdinalIgnoreCase)))
                {
                    error = BackupCreationError.DuplicateName;
                    return false;
                }
            }

            // Validate source if changed
            if (!string.Equals(newSource, existing.GetSourcePath(), StringComparison.OrdinalIgnoreCase))
            {
                if (!IsValidPath(newSource))
                {
                    error = BackupCreationError.SourcePathInvalid;
                    return false;
                }

                if (!Directory.Exists(newSource))
                {
                    error = BackupCreationError.SourcePathNotFound;
                    return false;
                }
            }

            // Validate target if changed
            if (!string.Equals(newTarget, existing.GetTargetPath(), StringComparison.OrdinalIgnoreCase))
            {
                if (!IsValidPath(newTarget))
                {
                    error = BackupCreationError.TargetPathInvalid;
                    return false;
                }

                if (!Directory.Exists(newTarget))
                {
                    error = BackupCreationError.TargetPathNotFound;
                    return false;
                }
            }

            // Replace job with updated values
            BackupJob updated = new BackupJob(newName, newSource, newTarget, type);
            backupJobs[index] = updated;
            SaveConfiguration();
            return true;
        }

        public bool UpdateBackupJob(int index, string? name, string? source, string? target, BackupType type)
        {
            return UpdateBackupJob(index, name, source, target, type, out _);
        }

        public void ExecuteBackupJob(int index)
        {
            index -= 1; // Adjust for 0-based index
            if (index < 0 || index >= backupJobs.Count) return;

            BackupJob job = backupJobs[index];
            // Read encryption settings from AppConfig (may be default disabled)
            var enc = AppConfig.Instance?.Encryption;
            bool encEnabled = enc?.Enabled ?? false;
            string encKey = enc?.Key ?? string.Empty;
            List<string>? encExt = enc?.Extensions;

            BackupExecutor executor = new BackupExecutor(logger, stateManager, businessMonitor, _largeFileThresholdKb, priorityExtensions, jobController, encEnabled, encKey, encExt);

            if (job.GetBackupType() == BackupType.Full)
                executor.ExecuteFullBackup(job);
            else
                executor.ExecuteDifferentialBackup(job);
        }

        public void ExecuteBackupJobs(int[] indices)
        {
            if (indices == null) return;
            foreach (var idx in indices)
            {
                ExecuteBackupJob(idx);
            }
        }

        public List<BackupJob> GetBackupJobs()
        {
            return backupJobs.ToList();
        }

        // Business Software Monitoring methods
        public BusinessSoftwareMonitor GetBusinessMonitor()
        {
            return businessMonitor;
        }

        public void AddMonitoredProcess(string processName)
        {
            businessMonitor.AddProcess(processName);
        }

        public void RemoveMonitoredProcess(string processName)
        {
            businessMonitor.RemoveProcess(processName);
        }

        public void ClearMonitoredProcesses()
        {
            businessMonitor.ClearProcesses();
        }

        public IReadOnlyList<string> GetMonitoredProcesses()
        {
            return businessMonitor.GetMonitoredProcesses();
        }

        public bool IsBusinessSoftwareRunning()
        {
            return businessMonitor.IsBusinessSoftwareRunning();
        }

        public List<string> GetRunningMonitoredProcesses()
        {
            return businessMonitor.GetRunningMonitoredProcesses();
        }

        // Priority Extensions Management
        public void AddPriorityExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return;

            string normalized = extension.Trim().ToLowerInvariant();
            if (!normalized.StartsWith(".")) normalized = "." + normalized;

            if (!priorityExtensions.Contains(normalized))
            {
                priorityExtensions.Add(normalized);
                SaveConfiguration();
            }
        }

        public void RemovePriorityExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return;

            string normalized = extension.Trim().ToLowerInvariant();
            if (priorityExtensions.Remove(normalized))
            {
                SaveConfiguration();
            }
        }

        public IReadOnlyList<string> GetPriorityExtensions()
        {
            return priorityExtensions.AsReadOnly();
        }

        public void ClearPriorityExtensions()
        {
            priorityExtensions.Clear();
            SaveConfiguration();
        }

        public void LoadConfiguration()
        {
            try
            {
                if (!File.Exists(configPath)) return;
                string json = File.ReadAllText(configPath);
                // Utilisation de l'instance statique
                var config = JsonSerializer.Deserialize<BackupConfiguration>(json, CachedJsonOptions);
                if (config == null) return;

                backupJobs.Clear();
                foreach (BackupConfigEntry e in config.BackupJobs ?? new List<BackupConfigEntry>())
                {
                    // determine log format if provided
                    EasyLog.LogFormat fmt = EasyLog.LogFormat.Json;
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(e.LogFormat) && string.Equals(e.LogFormat, "xml", StringComparison.OrdinalIgnoreCase))
                            fmt = EasyLog.LogFormat.Xml;
                    }
                    catch { }

                    BackupJob job = new BackupJob(e.Name, e.SourcePath, e.TargetPath, e.Type, fmt);
                    backupJobs.Add(job);
                }

                // Load priority extensions
                priorityExtensions.Clear();
                if (config.PriorityExtensions != null)
                {
                    foreach (var ext in config.PriorityExtensions)
                    {
                        if (!string.IsNullOrWhiteSpace(ext))
                        {
                            string normalized = ext.Trim().ToLowerInvariant();
                            if (!normalized.StartsWith(".")) normalized = "." + normalized;
                            if (!priorityExtensions.Contains(normalized))
                                priorityExtensions.Add(normalized);
                        }
                    }
                }
            }
            catch
            {
                // Ignore load errors
            }
        }

        public void SaveConfiguration()
        {
            try
            {
                var config = new BackupConfiguration
                {
                    BackupJobs = backupJobs.Select(b => new BackupConfigEntry
                    {
                        Name = b.GetName(),
                        SourcePath = b.GetSourcePath(),
                        TargetPath = b.GetTargetPath(),
                        Type = b.GetBackupType(),
                        LogFormat = b.GetLogFormat() == EasyLog.LogFormat.Xml ? "xml" : "json"
                    }).ToList(),
                    PriorityExtensions = priorityExtensions.ToList()
                };

                // Utilisation de l'instance statique
                string json = JsonSerializer.Serialize(config, CachedJsonOptions);
                File.WriteAllText(configPath, json);
            }
            catch
            {
                // Ignore save errors
            }
        }

        private class BackupConfiguration
        {
            public List<BackupConfigEntry> BackupJobs { get; set; } = new();
            public List<string> PriorityExtensions { get; set; } = new();
        }

        private class BackupConfigEntry
        {
            public required string Name { get; set; }
            public required string SourcePath { get; set; }
            public required string TargetPath { get; set; }
            public BackupType Type { get; set; }
            public string? LogFormat { get; set; }
        }
    }
}

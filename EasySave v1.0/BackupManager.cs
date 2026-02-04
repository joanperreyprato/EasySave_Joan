using EasyLog;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EasySave_v1._0
{
    public class BackupManager
    {
        private readonly List<BackupJob> backupJobs;
        private readonly StateManager stateManager;
        private readonly ILogger logger;

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

        public BackupManager(StateManager stateManager, ILogger logger, string? configPath = null)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.logger = logger;
            this.backupJobs = [];

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

            BackupJob job = new BackupJob(name, source, target, type);
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

        public void ExecuteBackupJob(int index)
        {
            index -= 1; // Adjust for 0-based index
            if (index < 0 || index >= backupJobs.Count) return;

            BackupJob job = backupJobs[index];
            BackupExecutor executor = new BackupExecutor(logger, stateManager);

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
                ExecuteBackupJob(idx-1);
            }
        }

        public List<BackupJob> GetBackupJobs()
        {
            return [.. backupJobs];
        }

        public void LoadConfiguration()
        {
            try
            {
                if (!File.Exists(configPath)) return;
                string json = File.ReadAllText(configPath);
                // Utilisation de l'instance statique
                List<BackupConfigEntry>? entries = JsonSerializer.Deserialize<List<BackupConfigEntry>>(json, CachedJsonOptions);
                if (entries == null) return;

                backupJobs.Clear();
                foreach (BackupConfigEntry e in entries)
                {
                    BackupJob job = new BackupJob(e.Name, e.SourcePath, e.TargetPath, e.Type);
                    backupJobs.Add(job);
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
                IEnumerable<BackupConfigEntry> entries = backupJobs.Select(b => new BackupConfigEntry
                {
                    Name = b.GetName(),
                    SourcePath = b.GetSourcePath(),
                    TargetPath = b.GetTargetPath(),
                    Type = b.GetBackupType()
                }).ToList();

                // Utilisation de l'instance statique
                string json = JsonSerializer.Serialize(entries, CachedJsonOptions);
                File.WriteAllText(configPath, json);
            }
            catch
            {
                // Ignore save errors
            }
        }

        private class BackupConfigEntry
        {
            public required string Name { get; set; }
            public required string SourcePath { get; set; }
            public required string TargetPath { get; set; }
            public BackupType Type { get; set; }
        }
    }
}

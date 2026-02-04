using EasyLog;
using System.Text.Json;

namespace EasySave_v1._0
{
    public class BackupManager
    {
        private readonly List<BackupJob> backupJobs;
        private readonly StateManager stateManager;
        private readonly ILogger logger;

        private readonly string configPath;

        // Ajout d'une instance statique pour JsonSerializerOptions
        private static readonly JsonSerializerOptions CachedJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public BackupManager(StateManager stateManager, ILogger logger, string? configPath = null)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.logger = logger;
            this.backupJobs = [];

            if (string.IsNullOrWhiteSpace(configPath))
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                var dir = Path.Combine(appData, "EasySave");
                Directory.CreateDirectory(dir);
                this.configPath = Path.Combine(dir, "backup_config.json");
            }
            else
            {
                this.configPath = configPath;
            }

            LoadConfiguration();
        }

        public bool CreateBackupJob(string name, string source, string target, BackupType type)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (backupJobs.Any(b => string.Equals(b.GetName(), name, StringComparison.OrdinalIgnoreCase)))
                return false; // duplicate name
            if (backupJobs.Count >= 5) return false; // limit to 5 jobs

            var job = new BackupJob(name, source, target, type);
            backupJobs.Add(job);
            SaveConfiguration();
            return true;
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
            if (index < 0 || index >= backupJobs.Count) return;

            var job = backupJobs[index];
            var executor = new BackupExecutor(logger, stateManager);

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
            return [.. backupJobs];
        }

        public void LoadConfiguration()
        {
            try
            {
                if (!File.Exists(configPath)) return;
                var json = File.ReadAllText(configPath);
                // Utilisation de l'instance statique
                var entries = JsonSerializer.Deserialize<List<BackupConfigEntry>>(json, CachedJsonOptions);
                if (entries == null) return;

                backupJobs.Clear();
                foreach (var e in entries)
                {
                    var job = new BackupJob(e.Name, e.SourcePath, e.TargetPath, e.Type);
                    backupJobs.Add(job);
                }
            }
            catch
            {
                // ignore load errors
            }
        }

        public void SaveConfiguration()
        {
            try
            {
                var entries = backupJobs.Select(b => new BackupConfigEntry
                {
                    Name = b.GetName(),
                    SourcePath = b.GetSourcePath(),
                    TargetPath = b.GetTargetPath(),
                    Type = b.GetBackupType()
                }).ToList();

                // Utilisation de l'instance statique
                var json = JsonSerializer.Serialize(entries, CachedJsonOptions);
                File.WriteAllText(configPath, json);
            }
            catch
            {
                // ignore save errors
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

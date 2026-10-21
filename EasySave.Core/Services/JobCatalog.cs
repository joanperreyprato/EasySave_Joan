using System.Text.Json;
using System.Text.RegularExpressions;
using EasySave.Core.Models;

namespace EasySave.Core.Services;

/// <summary>
/// Stores the configured backup jobs in a JSON file and validates new jobs.
/// Shared by every version of the backup service. Thread-safe.
/// </summary>
public sealed class JobCatalog
{
    private static readonly Regex NameRegex = new(@"^[\w\s\-]{1,50}$", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _configPath;
    private readonly int? _maxJobs;
    private readonly List<BackupJob> _jobs;
    private readonly object _lock = new();

    /// <summary>Loads the catalog from <paramref name="configPath"/>.</summary>
    /// <param name="configPath">The JSON file holding the jobs.</param>
    /// <param name="maxJobs">The maximum number of jobs, or <c>null</c> for unlimited.</param>
    public JobCatalog(string configPath, int? maxJobs)
    {
        _configPath = configPath;
        _maxJobs = maxJobs;
        _jobs = Load();
    }

    /// <summary>Validates and adds a new job, then persists the catalog.</summary>
    public BackupCreationResult Add(string name, string sourceDirectory, string targetDirectory, BackupType type)
    {
        lock (_lock)
        {
            if (_jobs.Count >= _maxJobs)
                return BackupCreationResult.MaxJobsExceeded;

            if (string.IsNullOrWhiteSpace(name) || !NameRegex.IsMatch(name))
                return BackupCreationResult.InvalidName;

            if (Find(name) is not null)
                return BackupCreationResult.DuplicateName;

            if (!Directory.Exists(sourceDirectory))
                return BackupCreationResult.SourceNotFound;

            // Target itself may not exist yet, but its parent must
            string? targetParent = Path.GetDirectoryName(targetDirectory.TrimEnd(Path.DirectorySeparatorChar));
            if (!string.IsNullOrEmpty(targetParent) && !Directory.Exists(targetParent))
                return BackupCreationResult.TargetNotFound;

            _jobs.Add(new BackupJob
            {
                Name = name,
                SourceDirectory = sourceDirectory,
                TargetDirectory = targetDirectory,
                Type = type
            });
            Save();
            return BackupCreationResult.Success;
        }
    }

    /// <summary>Removes the job with the given name.</summary>
    /// <returns><c>true</c> if a job was removed.</returns>
    public bool Remove(string name)
    {
        lock (_lock)
        {
            int removed = _jobs.RemoveAll(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (removed > 0) Save();
            return removed > 0;
        }
    }

    /// <summary>Returns a copy of all jobs in creation order.</summary>
    public List<BackupJob> GetAll()
    {
        lock (_lock) return [.. _jobs];
    }

    /// <summary>Returns the job with the given name (case-insensitive), or <c>null</c>.</summary>
    public BackupJob? Find(string name)
    {
        lock (_lock)
            return _jobs.Find(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private List<BackupJob> Load()
    {
        try
        {
            if (!File.Exists(_configPath)) return [];
            return JsonSerializer.Deserialize<List<BackupJob>>(File.ReadAllText(_configPath), JsonOptions) ?? [];
        }
        catch { return []; }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
        File.WriteAllText(_configPath, JsonSerializer.Serialize(_jobs, JsonOptions));
    }
}

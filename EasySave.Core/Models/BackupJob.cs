namespace EasySave.Core.Models;

/// <summary>
/// Represents a configured backup job with its name, source, target, and type.
/// </summary>
public sealed class BackupJob
{
    /// <summary>Gets or sets the unique name of the backup job (1–50 characters).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the full path to the source directory.</summary>
    public string SourceDirectory { get; set; } = string.Empty;

    /// <summary>Gets or sets the full path to the target directory.</summary>
    public string TargetDirectory { get; set; } = string.Empty;

    /// <summary>Gets or sets the type of backup (Full or Differential).</summary>
    public BackupType Type { get; set; } = BackupType.Full;
}

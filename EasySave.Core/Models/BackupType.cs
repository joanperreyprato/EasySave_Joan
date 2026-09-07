namespace EasySave.Core.Models;

/// <summary>Defines the type of backup operation.</summary>
public enum BackupType
{
    /// <summary>Copies all files from source to target directory.</summary>
    Full,
    /// <summary>Copies only files that are new or modified since the last backup.</summary>
    Differential
}

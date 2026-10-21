namespace EasySave.Core.Services;

/// <summary>
/// Features enabled for one backup run. The defaults reproduce the v1.0 behavior
/// (plain copy, no encryption, no priority, no pause/stop).
/// </summary>
public sealed class BackupRunOptions
{
    private static readonly IReadOnlySet<string> NoExtensions = new HashSet<string>();

    /// <summary>Gets the v1.0 options.</summary>
    public static BackupRunOptions Default { get; } = new();

    /// <summary>Gets the extensions (e.g. ".docx") encrypted with CryptoSoft after the copy (v2.0).</summary>
    public IReadOnlySet<string> EncryptionExtensions { get; init; } = NoExtensions;

    /// <summary>Gets the extensions transferred first, before any other file of any job (v3.0).</summary>
    public IReadOnlySet<string> PriorityExtensions { get; init; } = NoExtensions;

    /// <summary>Gets the size (KB) above which only one file may be transferred at a time; 0 = no limit (v3.0).</summary>
    public long LargeFileThresholdKb { get; init; }

    /// <summary>Gets the Play/Pause/Stop controller of the job, if any (v3.0).</summary>
    public JobController? Controller { get; init; }

    /// <summary>
    /// Gets a check run before each file: when it returns <c>true</c>, the job stops after the
    /// file in progress (v2.0 sequential behavior when the business software starts).
    /// </summary>
    public Func<bool>? InterruptRequested { get; init; }

    /// <summary>Builds a case-insensitive extension set (".TXT" and ".txt" are the same).</summary>
    public static IReadOnlySet<string> ToExtensionSet(IEnumerable<string> extensions)
        => new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
}

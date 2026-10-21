using EasySave.Core.Models;

namespace EasySave.WPF.ViewModels;

/// <summary>
/// ViewModel representing a single backup job in the WPF UI.
/// Wraps a <see cref="BackupJob"/> and adds observable progress/status properties.
/// </summary>
public sealed class JobViewModel : ViewModelBase
{
    private string _name = string.Empty;
    private string _sourceDirectory = string.Empty;
    private string _targetDirectory = string.Empty;
    private BackupType _type;
    private JobStatus _status = JobStatus.Inactive;
    private double _progress;
    private int _remainingFiles;
    private long _remainingSize;
    private bool _isSelected;

    /// <summary>Gets or sets the job name.</summary>
    public string Name { get => _name; set => SetField(ref _name, value); }

    /// <summary>Gets or sets the source directory path.</summary>
    public string SourceDirectory { get => _sourceDirectory; set => SetField(ref _sourceDirectory, value); }

    /// <summary>Gets or sets the target directory path.</summary>
    public string TargetDirectory { get => _targetDirectory; set => SetField(ref _targetDirectory, value); }

    /// <summary>Gets or sets the backup type (Full or Differential).</summary>
    public BackupType Type { get => _type; set => SetField(ref _type, value); }

    /// <summary>Gets or sets the current execution status.</summary>
    public JobStatus Status { get => _status; set { SetField(ref _status, value); OnPropertyChanged(nameof(StatusText)); OnPropertyChanged(nameof(IsRunning)); } }

    /// <summary>Gets or sets the completion progress (0–100).</summary>
    public double Progress { get => _progress; set => SetField(ref _progress, value); }

    /// <summary>Gets or sets the number of remaining files to transfer.</summary>
    public int RemainingFiles { get => _remainingFiles; set => SetField(ref _remainingFiles, value); }

    /// <summary>Gets or sets the remaining data size in bytes.</summary>
    public long RemainingSize { get => _remainingSize; set => SetField(ref _remainingSize, value); }

    /// <summary>Gets or sets whether this job is selected in the list.</summary>
    public bool IsSelected { get => _isSelected; set => SetField(ref _isSelected, value); }

    /// <summary>Gets the localized string representation of the current status.</summary>
    public string StatusText => EasySave.Core.Services.LanguageProvider.Instance.Get(Status switch
    {
        JobStatus.Active    => "status_active",
        JobStatus.Paused    => "status_paused",
        JobStatus.Completed => "status_completed",
        JobStatus.Error     => "status_error",
        JobStatus.Stopped   => "status_stopped",
        _                   => "status_inactive"
    });

    /// <summary>Forces UI to refresh translated texts.</summary>
    public void RefreshTranslations()
    {
        OnPropertyChanged(nameof(TypeText));
        OnPropertyChanged(nameof(StatusText));
    }

    /// <summary>Gets whether the job is currently running.</summary>
    public bool IsRunning => Status == JobStatus.Active;

    /// <summary>Gets the localized string representation of the backup type.</summary>
    public string TypeText => EasySave.Core.Services.LanguageProvider.Instance.Get(
        Type == BackupType.Full ? "job_type_full" : "job_type_differential");

    /// <summary>Creates a <see cref="JobViewModel"/> from a <see cref="BackupJob"/> model.</summary>
    public static JobViewModel FromModel(BackupJob job) => new()
    {
        Name = job.Name,
        SourceDirectory = job.SourceDirectory,
        TargetDirectory = job.TargetDirectory,
        Type = job.Type
    };
}

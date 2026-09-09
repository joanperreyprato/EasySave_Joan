using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Services;
using EasyLog;

namespace EasySave.WPF.ViewModels;

/// <summary>
/// Main ViewModel for the EasySave WPF application.
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    private readonly BackupServiceV2 _backupService;
    private readonly AppSettings _settings;
    private readonly BusinessSoftwareMonitor _monitor;
    private readonly StateManager _stateManager;
    private readonly DispatcherTimer _refreshTimer;

    /// <summary>The observable list of all configured backup jobs.</summary>
    public ObservableCollection<JobViewModel> Jobs { get; } = [];

    private bool _isDarkTheme;
    private string _statusMessage = string.Empty;
    private bool _isBusy;
    private JobViewModel? _selectedJob;
    private string _currentLanguage = "en";
    private string _businessSoftwareName = string.Empty;
    private string _encryptionExtensions = string.Empty;

    /// <summary>Command to run all selected jobs.</summary>
    public ICommand RunSelectedJobsCommand { get; }
    /// <summary>Command to run all jobs.</summary>
    public ICommand RunAllJobsCommand { get; }
    /// <summary>Command to delete the selected job.</summary>
    public ICommand DeleteJobCommand { get; }
    /// <summary>Command to toggle dark/light theme.</summary>
    public ICommand ToggleThemeCommand { get; }
    /// <summary>Command to toggle the UI language.</summary>
    public ICommand ToggleLanguageCommand { get; }
    /// <summary>Command to save settings.</summary>
    public ICommand SaveSettingsCommand { get; }

    /// <summary>Initializes the MainViewModel with all required services.</summary>
    public MainViewModel(BackupServiceV2 backupService, AppSettings settings,
        BusinessSoftwareMonitor monitor, StateManager stateManager)
    {
        _backupService = backupService;
        _settings = settings;
        _monitor = monitor;
        _stateManager = stateManager;

        _currentLanguage = settings.Language;
        _businessSoftwareName = settings.BusinessSoftwareName;
        _encryptionExtensions = string.Join(", ", settings.EncryptionExtensions);
        _monitor.ProcessName = _businessSoftwareName;

        RunSelectedJobsCommand = new RelayCommand(_ => RunSelectedJobs(), _ => !_isBusy);
        RunAllJobsCommand = new RelayCommand(_ => RunAllJobs(), _ => !_isBusy);
        DeleteJobCommand = new RelayCommand(_ => DeleteSelectedJob(), _ => _selectedJob != null && !_isBusy);
        ToggleThemeCommand = new RelayCommand(_ => ToggleTheme());
        ToggleLanguageCommand = new RelayCommand(_ => ToggleLanguage());
        SaveSettingsCommand = new RelayCommand(_ => SaveSettings());

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _refreshTimer.Tick += RefreshProgress;
        _refreshTimer.Start();

        RefreshJobs();
    }

    /// <summary>Gets or sets whether the dark theme is active.</summary>
    public bool IsDarkTheme { get => _isDarkTheme; set => SetField(ref _isDarkTheme, value); }

    /// <summary>Gets or sets the status bar message.</summary>
    public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }

    /// <summary>Gets or sets whether a backup is in progress.</summary>
    public bool IsBusy { get => _isBusy; set { SetField(ref _isBusy, value); OnPropertyChanged(nameof(IsNotBusy)); } }

    /// <summary>Gets whether no backup is in progress.</summary>
    public bool IsNotBusy => !_isBusy;

    /// <summary>Gets or sets the currently selected job.</summary>
    public JobViewModel? SelectedJob { get => _selectedJob; set => SetField(ref _selectedJob, value); }

    /// <summary>Gets or sets the business software process name to monitor.</summary>
    public string BusinessSoftwareName { get => _businessSoftwareName; set => SetField(ref _businessSoftwareName, value); }

    /// <summary>Gets or sets the comma-separated encryption extensions.</summary>
    public string EncryptionExtensions { get => _encryptionExtensions; set => SetField(ref _encryptionExtensions, value); }

    /// <summary>Gets the current language display text.</summary>
    public string CurrentLanguageText => _currentLanguage.ToUpper();

    /// <summary>Gets or sets the selected log format string ("JSON" or "XML").</summary>
    public string SelectedLogFormat
    {
        get => _settings.LogFormat;
        set => _settings.LogFormat = value;
    }

    /// <summary>Gets the theme toggle button text.</summary>
    public string ThemeButtonText => _isDarkTheme ? "☀ Light" : "🌙 Dark";

    /// <summary>Creates a new backup job and refreshes the list.</summary>
    public BackupCreationResult CreateJob(string name, string source, string target, BackupType type)
    {
        var result = _backupService.CreateJob(name, source, target, type);
        if (result == BackupCreationResult.Success) RefreshJobs();
        return result;
    }

    private void RunSelectedJobs()
    {
        var selected = Jobs.Where(j => j.IsSelected).Select(j => j.Name).ToList();
        if (selected.Count == 0) { StatusMessage = "⚠ No jobs selected."; return; }
        RunJobsAsync(selected);
    }

    private void RunAllJobs()
    {
        var all = Jobs.Select(j => j.Name).ToList();
        if (all.Count == 0) { StatusMessage = "⚠ No jobs configured."; return; }
        RunJobsAsync(all);
    }

    private void DeleteSelectedJob()
    {
        if (_selectedJob is null) return;
        _backupService.DeleteJob(_selectedJob.Name);
        RefreshJobs();
        StatusMessage = "Job deleted.";
    }

    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        ThemeManager.Apply(IsDarkTheme);
        OnPropertyChanged(nameof(ThemeButtonText));
    }

    private void ToggleLanguage()
    {
        _currentLanguage = _currentLanguage == "en" ? "fr" : "en";
        _settings.Language = _currentLanguage;
        OnPropertyChanged(nameof(CurrentLanguageText));
        StatusMessage = _currentLanguage == "fr" ? "Langue : Français" : "Language: English";
    }

    private void SaveSettings()
    {
        _settings.BusinessSoftwareName = BusinessSoftwareName;
        _monitor.ProcessName = BusinessSoftwareName;

        var extensions = EncryptionExtensions
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => e.StartsWith('.') ? e.ToLower() : $".{e.ToLower()}")
            .ToList();
        _settings.EncryptionExtensions = extensions;
        StatusMessage = "✓ Settings saved.";
    }

    private void RunJobsAsync(List<string> names)
    {
        if (_monitor.IsRunning())
        {
            StatusMessage = $"✗ Blocked: '{_settings.BusinessSoftwareName}' is running.";
            return;
        }

        IsBusy = true;
        StatusMessage = "▶ Running backups...";

        string logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "EasySave", "Logs");
        var format = _settings.LogFormat.Equals("XML", StringComparison.OrdinalIgnoreCase)
            ? LogFormat.Xml : LogFormat.Json;
        var logger = LoggerFactory.Create(format, logDir);

        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                _backupService.ExecuteJobs(names, logger);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = "✓ Backups completed.";
                    IsBusy = false;
                    RefreshJobs();
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $"✗ {ex.Message}";
                    IsBusy = false;
                    RefreshJobs();
                });
            }
        });
    }

    private void RefreshJobs()
    {
        var models = _backupService.GetAllJobs();
        Jobs.Clear();
        foreach (var m in models)
            Jobs.Add(JobViewModel.FromModel(m));
    }

    private void RefreshProgress(object? sender, EventArgs e)
    {
        if (!_isBusy) return;
        var states = _stateManager.GetAllStates();
        foreach (var job in Jobs)
        {
            var state = states.FirstOrDefault(s => s.Name == job.Name);
            if (state is null) continue;
            job.Status = state.Status;
            job.Progress = state.Progress;
            job.RemainingFiles = state.RemainingFiles;
            job.RemainingSize = state.RemainingSize;
        }
    }
}

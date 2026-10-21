using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using EasySave.Core.Models;
using EasySave.Core.Services;
using EasyLog;

namespace EasySave.WPF.ViewModels;

/// <summary>
/// Main ViewModel for the EasySave WPF application (v3.0).
/// Supports parallel execution, per-job and global Play/Pause/Stop, priority files,
/// large-file bandwidth control, log centralization and auto-pause on business software detection.
/// </summary>
public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly BackupServiceV3 _backupService;
    private readonly AppSettings _settings;
    private readonly BusinessSoftwareMonitor _monitor;
    private readonly StateManager _stateManager;
    private readonly DispatcherTimer _refreshTimer;

    /// <summary>The observable list of all configured backup jobs.</summary>
    public ObservableCollection<JobViewModel> Jobs { get; } = [];

    private bool   _isDarkTheme;
    private string _statusMessage        = string.Empty;
    private bool   _isBusy;
    private JobViewModel? _selectedJob;
    private string _currentLanguage;
    private string _businessSoftwareName;
    private string _encryptionExtensions;
    private string _priorityExtensions;
    private string _largeFileSizeKb;
    private string _logServer;

    // ── Commands ───────────────────────────────────────────────────────────────
    public ICommand RunSelectedJobsCommand { get; }
    public ICommand RunAllJobsCommand      { get; }
    public ICommand DeleteJobCommand       { get; }
    public ICommand ToggleThemeCommand     { get; }
    public ICommand ToggleLanguageCommand  { get; }
    public ICommand SaveSettingsCommand    { get; }

    // v3.0 — selected job controls (Play = start or resume)
    public ICommand PlayJobCommand  { get; }
    public ICommand PauseJobCommand { get; }
    public ICommand StopJobCommand  { get; }

    // v3.0 — global controls
    public ICommand PlayAllCommand  { get; }
    public ICommand PauseAllCommand { get; }
    public ICommand StopAllCommand  { get; }

    // ── Constructor ────────────────────────────────────────────────────────────
    public MainViewModel(BackupServiceV3 backupService, AppSettings settings,
        BusinessSoftwareMonitor monitor, StateManager stateManager)
    {
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        _settings      = settings      ?? throw new ArgumentNullException(nameof(settings));
        _monitor       = monitor       ?? throw new ArgumentNullException(nameof(monitor));
        _stateManager  = stateManager  ?? throw new ArgumentNullException(nameof(stateManager));

        _currentLanguage      = settings.Language;
        _businessSoftwareName = settings.BusinessSoftwareName;
        _encryptionExtensions = string.Join(", ", settings.EncryptionExtensions);
        _priorityExtensions   = string.Join(", ", settings.PriorityExtensions);
        _largeFileSizeKb      = settings.MaxParallelFileSizeKb.ToString();
        _logServer            = $"{settings.LogServerAddress}:{settings.LogServerPort}";
        _monitor.ProcessName  = _businessSoftwareName;
        LanguageProvider.Instance.SetLanguage(_currentLanguage);

        RunSelectedJobsCommand = new RelayCommand(_ => RunSelectedJobs());
        RunAllJobsCommand      = new RelayCommand(_ => RunJobs(Jobs.Select(j => j.Name).ToList(), "status_no_jobs"));
        DeleteJobCommand       = new RelayCommand(_ => DeleteSelectedJob(), _ => _selectedJob != null && !IsSelectedJobRunning);
        ToggleThemeCommand     = new RelayCommand(_ => ToggleTheme());
        ToggleLanguageCommand  = new RelayCommand(_ => ToggleLanguage());
        SaveSettingsCommand    = new RelayCommand(_ => SaveSettings());

        PlayJobCommand  = new RelayCommand(_ => PlaySelectedJob(), _ => _selectedJob != null);
        PauseJobCommand = new RelayCommand(_ => _backupService.PauseJob(_selectedJob!.Name), _ => IsSelectedJobRunning);
        StopJobCommand  = new RelayCommand(_ => _backupService.StopJob(_selectedJob!.Name),  _ => IsSelectedJobRunning);

        PlayAllCommand  = new RelayCommand(_ => _backupService.ResumeAll(), _ => _isBusy);
        PauseAllCommand = new RelayCommand(_ => _backupService.PauseAll(),  _ => _isBusy);
        StopAllCommand  = new RelayCommand(_ => _backupService.StopAll(),   _ => _isBusy);

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _refreshTimer.Tick += RefreshProgress;
        _refreshTimer.Start();

        RefreshJobs();
    }

    // ── Properties ─────────────────────────────────────────────────────────────
    public bool          IsDarkTheme          { get => _isDarkTheme;          set => SetField(ref _isDarkTheme, value); }
    public string        StatusMessage        { get => _statusMessage;        set => SetField(ref _statusMessage, value); }
    public bool          IsBusy               { get => _isBusy;               set { SetField(ref _isBusy, value); OnPropertyChanged(nameof(IsNotBusy)); } }
    public bool          IsNotBusy            => !_isBusy;
    public JobViewModel? SelectedJob          { get => _selectedJob;          set => SetField(ref _selectedJob, value); }
    public string        BusinessSoftwareName { get => _businessSoftwareName; set => SetField(ref _businessSoftwareName, value); }
    public string        EncryptionExtensions { get => _encryptionExtensions; set => SetField(ref _encryptionExtensions, value); }
    public string        PriorityExtensions   { get => _priorityExtensions;   set => SetField(ref _priorityExtensions, value); }
    public string        LargeFileSizeKb      { get => _largeFileSizeKb;      set => SetField(ref _largeFileSizeKb, value); }
    public string        LogServer            { get => _logServer;            set => SetField(ref _logServer, value); }
    public string        CurrentLanguageText  => _currentLanguage.ToUpper();
    public string        ThemeButtonText      => this[_isDarkTheme ? "btn_theme_light" : "btn_theme_dark"];
    public string        this[string key]     => LanguageProvider.Instance.Get(key);

    /// <summary>Gets or sets the log file format ("Json" or "Xml"), saved immediately.</summary>
    public string SelectedLogFormat
    {
        get => _settings.LogFileFormat.ToString();
        set { _settings.LogFormat = value; OnPropertyChanged(); }
    }

    /// <summary>Gets or sets the log destination ("Local", "Centralized" or "Both"), saved immediately.</summary>
    public string SelectedLogMode
    {
        get => _settings.LogMode.ToString();
        set
        {
            if (Enum.TryParse(value, out LogMode mode)) _settings.LogMode = mode;
            OnPropertyChanged();
        }
    }

    private bool IsSelectedJobRunning => _selectedJob != null && _backupService.IsJobRunning(_selectedJob.Name);

    // ── Public API ─────────────────────────────────────────────────────────────
    public BackupCreationResult CreateJob(string name, string source, string target, BackupType type)
    {
        var result = _backupService.CreateJob(name, source, target, type);
        if (result == BackupCreationResult.Success) RefreshJobs();
        return result;
    }

    // ── Commands implementations ───────────────────────────────────────────────

    private void RunSelectedJobs()
        => RunJobs(Jobs.Where(j => j.IsSelected).Select(j => j.Name).ToList(), "status_no_selection");

    /// <summary>Play: resumes the selected job if it is running, starts it otherwise.</summary>
    private void PlaySelectedJob()
    {
        if (IsSelectedJobRunning) _backupService.ResumeJob(_selectedJob!.Name);
        else RunJobs([_selectedJob!.Name], "status_no_selection");
    }

    private void DeleteSelectedJob()
    {
        if (_selectedJob is null) return;
        _backupService.DeleteJob(_selectedJob.Name);
        RefreshJobs();
        StatusMessage = this["status_job_deleted"];
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
        LanguageProvider.Instance.SetLanguage(_currentLanguage);
        OnPropertyChanged(nameof(CurrentLanguageText));
        OnPropertyChanged(nameof(ThemeButtonText));
        OnPropertyChanged("Item[]");
        foreach (var job in Jobs) job.RefreshTranslations();
        StatusMessage = this["status_language"];
    }

    private void SaveSettings()
    {
        _settings.BusinessSoftwareName = BusinessSoftwareName;
        _monitor.ProcessName           = BusinessSoftwareName;

        _settings.EncryptionExtensions = ParseExtensions(EncryptionExtensions);
        _settings.PriorityExtensions   = ParseExtensions(PriorityExtensions);

        if (long.TryParse(LargeFileSizeKb, out long size))
            _settings.MaxParallelFileSizeKb = size;

        var server = LogServer.Split(':', 2, StringSplitOptions.TrimEntries);
        if (server[0].Length > 0) _settings.LogServerAddress = server[0];
        if (server.Length == 2 && int.TryParse(server[1], out int port)) _settings.LogServerPort = port;

        StatusMessage = this["status_settings_saved"];
    }

    /// <summary>Starts the given jobs in parallel (jobs already running are ignored).</summary>
    private void RunJobs(List<string> names, string emptyMessageKey)
    {
        if (names.Count == 0) { StatusMessage = this[emptyMessageKey]; return; }

        IsBusy = true;
        StatusMessage = this["status_running"];
        var logger = _settings.CreateLogger();

        Task.Run(async () =>
        {
            string message;
            try
            {
                await _backupService.ExecuteJobsParallelAsync(names, logger);
                message = this["status_all_completed"];
            }
            catch (Exception ex)
            {
                message = $"✗ {ex.Message}";
            }
            finally
            {
                (logger as IDisposable)?.Dispose();
            }

            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                if (!_backupService.HasRunningJobs) StatusMessage = message;
                RefreshProgress(null, EventArgs.Empty);
            });
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
        var states = _stateManager.GetAllStates();
        foreach (var job in Jobs)
        {
            var state = states.FirstOrDefault(s => s.Name == job.Name);
            if (state is null) continue;
            job.Status         = state.Status;
            job.Progress       = state.Progress;
            job.RemainingFiles = state.RemainingFiles;
            job.RemainingSize  = state.RemainingSize;
        }

        IsBusy = _backupService.HasRunningJobs;
        if (_isBusy && _backupService.IsBusinessSoftwareRunning)
            StatusMessage = this["status_business_paused"];
        else if (_isBusy && StatusMessage == this["status_business_paused"])
            StatusMessage = this["status_running"];
        CommandManager.InvalidateRequerySuggested();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
    private static List<string> ParseExtensions(string raw) =>
        raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
           .Select(e => e.StartsWith('.') ? e.ToLower() : $".{e.ToLower()}")
           .ToList();

    public void Dispose()
    {
        _refreshTimer.Stop();
        _backupService.Dispose();
    }
}

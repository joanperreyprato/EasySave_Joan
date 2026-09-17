using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using EasySave.Core.Models;
using EasySave.Core.Services;
using EasyLog;

namespace EasySave.WPF.ViewModels;

/// <summary>
/// Main ViewModel for the EasySave WPF application (v3.0).
/// Supports parallel execution, per-job Play/Pause/Stop, priority files,
/// large-file bandwidth control, and auto-pause on business software detection.
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
    private string _statusMessage       = string.Empty;
    private bool   _isBusy;
    private JobViewModel? _selectedJob;
    private string _currentLanguage     = "en";
    private string _businessSoftwareName = string.Empty;
    private string _encryptionExtensions = string.Empty;
    private string _priorityExtensions   = string.Empty;
    private string _largeFileSizeKb      = "1024";

    // ── Commands ───────────────────────────────────────────────────────────────
    public ICommand RunSelectedJobsCommand { get; }
    public ICommand RunAllJobsCommand      { get; }
    public ICommand DeleteJobCommand       { get; }
    public ICommand ToggleThemeCommand     { get; }
    public ICommand ToggleLanguageCommand  { get; }
    public ICommand SaveSettingsCommand    { get; }

    // v3.0 — per-job controls
    public ICommand PauseJobCommand  { get; }
    public ICommand ResumeJobCommand { get; }
    public ICommand StopJobCommand   { get; }

    // v3.0 — global controls
    public ICommand PauseAllCommand  { get; }
    public ICommand ResumeAllCommand { get; }
    public ICommand StopAllCommand   { get; }

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
        _monitor.ProcessName  = _businessSoftwareName;

        // Global commands
        RunSelectedJobsCommand = new RelayCommand(_ => RunSelectedJobs(), _ => !_isBusy);
        RunAllJobsCommand      = new RelayCommand(_ => RunAllJobs(),      _ => !_isBusy);
        DeleteJobCommand       = new RelayCommand(_ => DeleteSelectedJob(), _ => _selectedJob != null);
        ToggleThemeCommand     = new RelayCommand(_ => ToggleTheme());
        ToggleLanguageCommand  = new RelayCommand(_ => ToggleLanguage());
        SaveSettingsCommand    = new RelayCommand(_ => SaveSettings());

        // Per-job commands (v3.0)
        PauseJobCommand  = new RelayCommand(_ => _backupService.PauseJob(_selectedJob!.Name),  _ => _selectedJob != null && _isBusy);
        ResumeJobCommand = new RelayCommand(_ => _backupService.ResumeJob(_selectedJob!.Name), _ => _selectedJob != null && _isBusy);
        StopJobCommand   = new RelayCommand(_ => _backupService.StopJob(_selectedJob!.Name),   _ => _selectedJob != null && _isBusy);

        // Global controls (v3.0)
        PauseAllCommand  = new RelayCommand(_ => _backupService.PauseAll(),  _ => _isBusy);
        ResumeAllCommand = new RelayCommand(_ => _backupService.ResumeAll(), _ => _isBusy);
        StopAllCommand   = new RelayCommand(_ => _backupService.StopAll(),   _ => _isBusy);

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
    public string        CurrentLanguageText  => _currentLanguage.ToUpper();
    public string        ThemeButtonText      => _isDarkTheme ? "☀ Light" : "🌙 Dark";
    public string        SelectedLogFormat    { get => _settings.LogFormat; set => _settings.LogFormat = value; }
    public string        this[string key]     => LanguageProvider.Instance.Get(key);

    // ── Public API ─────────────────────────────────────────────────────────────
    public BackupCreationResult CreateJob(string name, string source, string target, BackupType type)
    {
        var result = _backupService.CreateJob(name, source, target, type);
        if (result == BackupCreationResult.Success) RefreshJobs();
        return result;
    }

    // ── Commands implementations ───────────────────────────────────────────────

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
        LanguageProvider.Instance.SetLanguage(_currentLanguage);
        OnPropertyChanged(nameof(CurrentLanguageText));
        OnPropertyChanged("Item[]");
        foreach (var job in Jobs) job.RefreshTranslations();
        StatusMessage = _currentLanguage == "fr" ? "Langue : Français" : "Language: English";
    }

    private void SaveSettings()
    {
        _settings.BusinessSoftwareName = BusinessSoftwareName;
        _monitor.ProcessName           = BusinessSoftwareName;

        _settings.EncryptionExtensions = ParseExtensions(EncryptionExtensions);
        _settings.PriorityExtensions   = ParseExtensions(PriorityExtensions);

        if (long.TryParse(LargeFileSizeKb, out long size))
            _settings.MaxParallelFileSizeKb = size;

        StatusMessage = "✓ Settings saved.";
    }

    private void RunJobsAsync(List<string> names)
    {
        IsBusy = true;
        StatusMessage = "▶ Running backups in parallel...";

        string logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "EasySave", "Logs");
        var format = _settings.LogFormat.Equals("XML", StringComparison.OrdinalIgnoreCase)
            ? LogFormat.Xml : LogFormat.Json;
        var logger = LoggerFactory.Create(format, logDir);

        System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                await _backupService.ExecuteJobsParallelAsync(names, logger);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = "✓ All backups completed.";
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
        var states = _stateManager.GetAllStates();
        bool anyActive = false;
        foreach (var job in Jobs)
        {
            var state = states.FirstOrDefault(s => s.Name == job.Name);
            if (state is null) continue;
            job.Status         = state.Status;
            job.Progress       = state.Progress;
            job.RemainingFiles = state.RemainingFiles;
            job.RemainingSize  = state.RemainingSize;
            if (state.Status == JobStatus.Active || state.Status == JobStatus.Paused)
                anyActive = true;
        }
        if (_isBusy && !anyActive)
        {
            IsBusy = false;
            if (!StatusMessage.StartsWith("✓")) StatusMessage = "✓ All backups completed.";
        }
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

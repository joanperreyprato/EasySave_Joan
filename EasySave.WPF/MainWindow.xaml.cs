using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using EasySave_v1._0;
using EasyLog;
using System.Collections.Generic;

namespace EasySave.WPF
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<JobViewModel> Jobs { get; } = new();

        readonly StateManager _stateManager;
        readonly BackupManager _backupManager;
        readonly ILogger _logger;
        readonly string _logDir;
        List<BackupJob> _backupJobs = new();

        public MainWindow()
        {
            InitializeComponent();
            JobsList.ItemsSource = Jobs;

            // Initialize UI language using core LanguageProvider
            var lp = EasySave_v1._0.LanguageProvider.GetInstance();

            // Initialize core services from library projects
            string logDir;
            try
            {
                logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EasySave", "logs");
            }
            catch
            {
                logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            }
            Directory.CreateDirectory(logDir);

            _stateManager = new StateManager();
            _stateManager.LoadState();

            _logger = new JsonLogger(logDir);
            _logDir = logDir;

            // Prefer existing config file if present (project root or common appdata)
            string appDataConfig = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EasySave", "backup_config.json");
            string localConfig = Path.Combine(Directory.GetCurrentDirectory(), "backup_config.json");
            string? chosenConfig = null;
            if (File.Exists(localConfig)) chosenConfig = localConfig;
            else if (File.Exists(appDataConfig)) chosenConfig = appDataConfig;

            if (chosenConfig != null)
                _backupManager = new BackupManager(_stateManager, _logger, chosenConfig);
            else
                _backupManager = new BackupManager(_stateManager, logger: _logger);

            // load existing backup jobs from config
            _backupJobs = _backupManager.GetBackupJobs();
            foreach (var b in _backupJobs)
            {
                Jobs.Add(new JobViewModel { Name = b.GetName(), Source = b.GetSourcePath(), Destination = b.GetTargetPath(), State = "Stopped", PreferredLogFormat = b.GetLogFormat() });
            }

            BtnAddJob.Click -= (_, __) => AddJob();
            BtnStartAll.Click -= (_, __) => StartAll();
            BtnStopAll.Click -= (_, __) => StopAll();
            BtnStartSelected.Click -= (_, __) => StartSelected();
            BtnStopSelected.Click -= (_, __) => StopSelected();
            CbLanguage.SelectionChanged -= (_, __) => ChangeLanguage();

            BtnAddJob.Click += (_, __) => AddJob();
            BtnStartAll.Click += (_, __) => StartAll();
            BtnStopAll.Click += (_, __) => StopAll();
            BtnStartSelected.Click += (_, __) => StartSelected();
            BtnStopSelected.Click += (_, __) => StopSelected();
            CbLanguage.SelectionChanged += (_, __) => ChangeLanguage();
            // select combobox to current language
            try
            {
                CbLanguage.SelectedIndex = string.Equals(lp.GetText("language.english"), "English", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
            }
            catch { CbLanguage.SelectedIndex = 0; }

            // Apply UI texts after initialization
            ApplyLanguage(lp);
            void ChangeLanguage()
            {
                if (CbLanguage.SelectedItem is System.Windows.Controls.ComboBoxItem it && it.Tag is string code)
                {
                    var lp = EasySave_v1._0.LanguageProvider.GetInstance();
                    lp.SetLanguage(code);
                    ApplyLanguage(lp);

                    // Reload jobs from configuration to avoid duplicates and reflect any configuration changes
                    Jobs.Clear();
                    _backupJobs = _backupManager.GetBackupJobs();
                    foreach (var b in _backupJobs)
                    {
                        Jobs.Add(new JobViewModel { Name = b.GetName(), Source = b.GetSourcePath(), Destination = b.GetTargetPath(), State = "Stopped", PreferredLogFormat = b.GetLogFormat() });
                    }
                }
            }

            void ApplyLanguage(EasySave_v1._0.LanguageProvider lp)
            {
                // Only update UI texts here. Do not modify Jobs collection or reattach handlers.
                this.Title = lp.GetText("window.title");
                BtnAddJob.Content = lp.GetText("btn.add");
                BtnStartAll.Content = lp.GetText("btn.startall");
                BtnStopAll.Content = lp.GetText("btn.stopall");
                BtnStartSelected.Content = lp.GetText("btn.startsel");
                BtnStopSelected.Content = lp.GetText("btn.stopsel");
                StatusText.Text = lp.GetText("status.ready");
            // update column headers
            try
            {
                ColNameHeader.Text = lp.GetText("col.name");
                ColSourceHeader.Text = lp.GetText("col.source");
                ColTargetHeader.Text = lp.GetText("col.target");
                ColStateHeader.Text = lp.GetText("col.state");
            }
            catch { }
            }

            void AddJob()
            {
                var dlg = new AddJobWindow();
                if (dlg.ShowDialog() == true && dlg.Job != null)
                {
                    // create in core BackupManager (it will validate and persist)
                    if (_backupManager.CreateBackupJob(dlg.Job.Name, dlg.Job.Source, dlg.Job.Destination, BackupType.Full, dlg.Job.PreferredLogFormat, out var err))
                    {
                        // reload backing list and UI
                        _backupJobs = _backupManager.GetBackupJobs();
                        Jobs.Add(new JobViewModel { Name = dlg.Job.Name, Source = dlg.Job.Source, Destination = dlg.Job.Destination, State = "Stopped", PreferredLogFormat = dlg.Job.PreferredLogFormat });
                    }
                    else
                    {
                        MessageBox.Show($"Failed to create job: {err}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            void StartAll()
            {
                for (int i = 0; i < _backupJobs.Count; i++)
                {
                    StartBackupByIndex(i + 1);
                }
                StatusText.Text = "Tous les travaux sont lancés";
            }

            void StartSelected()
            {
                foreach (var item in JobsList.SelectedItems.Cast<object>())
                {
                    if (item is JobViewModel vm)
                    {
                        // find matching backup job index
                        int idx = _backupJobs.FindIndex(b => b.GetName() == vm.Name && b.GetSourcePath() == vm.Source && b.GetTargetPath() == vm.Destination);
                        if (idx >= 0) StartBackupByIndex(idx + 1);
                    }
                }
                StatusText.Text = "Travaux sélectionnés lancés";
            }

            void StopSelected()
            {
                // BackupExecutor does not support cancellation in this codebase; notify the user
                MessageBox.Show("Arrêt immédiat non supporté pour les sauvegardes en cours.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            void StopAll()
            {
                // Similarly, no cancellation available
                MessageBox.Show("Arrêt immédiat non supporté pour les sauvegardes en cours.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            void StartBackupByIndex(int oneBasedIndex)
            {
                // set UI state and run in background without progress polling
                var b = _backupJobs.ElementAtOrDefault(oneBasedIndex - 1);
                if (b == null) return;

                var vm = Jobs.FirstOrDefault(j => j.Name == b.GetName() && j.Source == b.GetSourcePath() && j.Destination == b.GetTargetPath());
                if (vm != null) vm.State = "Running";

                _ = Task.Run(() =>
                {
                    try
                    {
                        _backupManager.ExecuteBackupJob(oneBasedIndex);
                    }
                    catch { }
                    finally
                    {
                        Dispatcher.Invoke(() => { if (vm != null) vm.State = "Stopped"; });
                    }
                });
            }
        }
    }
}
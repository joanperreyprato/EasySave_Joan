using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using EasySave.Core;
using EasyLog;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Threading;

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
        // Track running backup tasks for parallel execution
        readonly Dictionary<string, Task> _runningTasks = new();
        // Timer for updating job progress
        readonly DispatcherTimer _progressUpdateTimer;
        // encryption key from UI (may be empty)
        public string EncryptionKey => PbEncryptionKey?.Password ?? string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            JobsList.ItemsSource = Jobs;

            // Initialize UI language using core LanguageProvider
            var lp = EasySave.Core.LanguageProvider.GetInstance();

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

            // Toggle dropdown when clicking anywhere in the language combo box

            Directory.CreateDirectory(logDir);

            _stateManager = new StateManager();
            _stateManager.LoadState();

            // Create logger based on configuration (supports local + remote logging)
            _logger = LoggerFactory.CreateFromConfig(logDir);
            _logDir = logDir;

            // Prefer existing config file if present (project root or common appdata)
            string appDataConfig = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EasySave", "backup_config.json");
            string localConfig = Path.Combine(Directory.GetCurrentDirectory(), "backup_config.json");
            string? chosenConfig = null;
            if (File.Exists(localConfig)) chosenConfig = localConfig;
            else if (File.Exists(appDataConfig)) chosenConfig = appDataConfig;

            // Read large-file threshold (KB) from UI; default to 0 if parsing fails or control missing
            int largeFileThresholdKb = 0;
            if (TbLargeFileThresholdKb != null)
            {
                int.TryParse(TbLargeFileThresholdKb.Text?.Trim() ?? string.Empty, out largeFileThresholdKb);
            }

            if (chosenConfig != null)
                _backupManager = new BackupManager(_stateManager, _logger, chosenConfig, largeFileThresholdKb);
            else
                _backupManager = new BackupManager(_stateManager, logger: _logger, largeFileThresholdKb: largeFileThresholdKb);

            // load existing backup jobs from config
            _backupJobs = _backupManager.GetBackupJobs();
            foreach (var b in _backupJobs)
            {
                Jobs.Add(new JobViewModel { Name = b.GetName(), Source = b.GetSourcePath(), Destination = b.GetTargetPath(), State = "Stopped", PreferredLogFormat = b.GetLogFormat() });
            }

            // Ensure threshold textbox has a default value for clarity
            if (TbLargeFileThresholdKb != null && string.IsNullOrWhiteSpace(TbLargeFileThresholdKb.Text))
            {
                TbLargeFileThresholdKb.Text = largeFileThresholdKb.ToString();
            }

            BtnAddJob.Click -= (_, __) => AddJob();
            BtnStartAll.Click -= (_, __) => StartAll();
            BtnStopAll.Click -= (_, __) => StopAll();
            BtnStartSelected.Click -= (_, __) => StartSelected();
            BtnStopSelected.Click -= (_, __) => StopSelected();
            BtnPauseSelected.Click -= (_, __) => PauseSelected();
            BtnPauseAll.Click -= (_, __) => PauseAll();
            CbLanguage.SelectionChanged -= (_, __) => ChangeLanguage();

            BtnAddJob.Click += (_, __) => AddJob();
            BtnStartAll.Click += (_, __) => StartAll();
            BtnStopAll.Click += (_, __) => StopAll();
            BtnStartSelected.Click += (_, __) => StartSelected();
            BtnStopSelected.Click += (_, __) => StopSelected();
            BtnPauseSelected.Click += (_, __) => PauseSelected();
            BtnPauseAll.Click += (_, __) => PauseAll();
            CbLanguage.SelectionChanged += (_, __) => ChangeLanguage();

            // Initialize and start the progress update timer
            _progressUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500) // Update every 500ms
            };
            _progressUpdateTimer.Tick += UpdateJobsProgress;
            _progressUpdateTimer.Start();
            BtnStopSelected.Click += (_, __) => StopSelected();
            CbLanguage.SelectionChanged += (_, __) => ChangeLanguage();
            // select combobox to current language
            try
            {
                CbLanguage.SelectedValue = lp.CurrentLanguage;
            }
            catch { CbLanguage.SelectedIndex = 0; }

            // Apply UI texts after initialization
            ApplyLanguage(lp);
            // Wire up encryption list handlers
            BtnAddExt.Click -= (_, __) => { };
            BtnRemoveExt.Click -= (_, __) => { };
            BtnAddExt.Click += (_, __) =>
            {
                string ext = TbNewExt.Text?.Trim();
                if (string.IsNullOrWhiteSpace(ext)) return;
                if (!ext.StartsWith('.')) ext = "." + ext;
                if (!LbEncryptionTypes.Items.Contains(ext)) LbEncryptionTypes.Items.Add(ext);
                TbNewExt.Text = string.Empty;
            };
            BtnRemoveExt.Click += (_, __) =>
            {
                if (LbEncryptionTypes.SelectedItem != null)
                {
                    LbEncryptionTypes.Items.Remove(LbEncryptionTypes.SelectedItem);
                }
            };

            // Wire up business software monitoring handlers
            LoadBusinessProcesses();

            BtnAddProcess.Click += (_, __) =>
            {
                string process = TbNewProcess.Text?.Trim();
                if (string.IsNullOrWhiteSpace(process)) return;

                _backupManager.AddMonitoredProcess(process);
                LoadBusinessProcesses();
                UpdateBusinessStatus();
                TbNewProcess.Text = string.Empty;
            };

            BtnRemoveProcess.Click += (_, __) =>
            {
                if (LbBusinessProcesses.SelectedItem is string selectedProcess)
                {
                    _backupManager.RemoveMonitoredProcess(selectedProcess);
                    LoadBusinessProcesses();
                    UpdateBusinessStatus();
                }
            };

            BtnAddCalc.Click += (_, __) =>
            {
                _backupManager.AddMonitoredProcess("CalculatorApp");
                LoadBusinessProcesses();
                UpdateBusinessStatus();
            };

            BtnAddNotepad.Click += (_, __) =>
            {
                _backupManager.AddMonitoredProcess("notepad");
                LoadBusinessProcesses();
                UpdateBusinessStatus();
            };

            BtnRefreshStatus.Click += (_, __) =>
            {
                UpdateBusinessStatus();
            };

            // Wire up priority extensions handlers
            LoadPriorityExtensions();

            BtnAddPriorityExt.Click += (_, __) =>
            {
                string ext = TbNewPriorityExt.Text?.Trim();
                if (string.IsNullOrWhiteSpace(ext)) return;
                if (!ext.StartsWith('.')) ext = "." + ext;

                _backupManager.AddPriorityExtension(ext);
                LoadPriorityExtensions();
                TbNewPriorityExt.Text = string.Empty;
            };

            BtnRemovePriorityExt.Click += (_, __) =>
            {
                if (LbPriorityExtensions.SelectedItem is string selectedExt)
                {
                    _backupManager.RemovePriorityExtension(selectedExt);
                    LoadPriorityExtensions();
                }
            };

            // Wire up remote logging handlers
            LoadRemoteLoggingConfig();

            CbEnableRemoteLogging.Checked += (_, __) =>
            {
                PnlRemoteLoggingSettings.IsEnabled = true;
                PnlRemoteLoggingSettings.Opacity = 1.0;
            };

            CbEnableRemoteLogging.Unchecked += (_, __) =>
            {
                PnlRemoteLoggingSettings.IsEnabled = false;
                PnlRemoteLoggingSettings.Opacity = 0.5;
            };

            BtnTestConnection.Click += async (_, __) =>
            {
                await TestRemoteLoggingConnection();
            };

            // Save remote logging config on window closing
            this.Closing += (_, __) =>
            {
                SaveRemoteLoggingConfig();
            };

            // Localize add/remove buttons and list description
            try
            {
                var l = EasySave.Core.LanguageProvider.GetInstance();
                BtnAddExt.Content = l.GetText("encryption.add");
                BtnRemoveExt.Content = l.GetText("encryption.remove");
                LblEncryptionListDesc.Text = l.GetText("encryption.list.desc");
            }
            catch { }
            void ChangeLanguage()
            {
                if (CbLanguage.SelectedValue is string code)
                {
                    var lp = EasySave.Core.LanguageProvider.GetInstance();
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

            void ApplyLanguage(EasySave.Core.LanguageProvider lp)
            {
                // Only update UI texts here. Do not modify Jobs collection or reattach handlers.
                this.Title = lp.GetText("window.title");
                HeaderSubtitle.Text = lp.GetText("header.subtitle");
                HeaderBadgeFast.Text = lp.GetText("header.badge.fast");
                LblLanguage.Text = lp.GetText("label.language");
                TabJobsHeader.Text = lp.GetText("tab.jobs");
                JobsSectionTitle.Text = lp.GetText("jobs.section.title");
                JobsSectionDesc.Text = lp.GetText("jobs.section.desc");
                BtnAddJob.Content = lp.GetText("btn.add");
                BtnStartAll.Content = lp.GetText("btn.startall");
                BtnStopAll.Content = lp.GetText("btn.stopall");
                BtnStartSelected.Content = lp.GetText("btn.startsel");
                BtnStopSelected.Content = lp.GetText("btn.stopsel");
                BtnPauseSelected.Content = lp.GetText("btn.pausesel");
                BtnPauseAll.Content = lp.GetText("btn.pauseall");
                StatusText.Text = lp.GetText("status.ready");
                // update column headers
                try
                {
                    ColNameHeader.Text = lp.GetText("col.name");
                    ColSourceHeader.Text = lp.GetText("col.source");
                    ColTargetHeader.Text = lp.GetText("col.target");
                    ColStateHeader.Text = lp.GetText("col.state");
                    ColProgressHeader.Text = lp.GetText("col.progress");
                    ColActionsHeader.Text = lp.GetText("col.actions");
                    // Update encryption tab texts
                    try
                    {
                        LblEncryptionHeader.Text = lp.GetText("encryption.code.label");
                        TbEncryptionDesc.Text = lp.GetText("encryption.description");
                        SecuritySectionTitle.Text = lp.GetText("security.section.title");
                        SecuritySectionDesc.Text = lp.GetText("security.section.desc");
                        EncryptionExtensionsTitle.Text = lp.GetText("encryption.extensions.title");
                        // Update tab header if translation exists
                        string tabHeader = lp.GetText("tab.encryption");
                        if (!string.IsNullOrWhiteSpace(tabHeader) && tabHeader != "tab.encryption")
                            TabEncryption.Header = tabHeader;
                        // Update add/remove button texts and list description
                        try
                        {
                            BtnAddExt.Content = lp.GetText("encryption.add");
                            BtnRemoveExt.Content = lp.GetText("encryption.remove");
                            LblEncryptionListDesc.Text = lp.GetText("encryption.list.desc");
                        }
                        catch { }

                        // Update priority extensions texts
                        try
                        {
                            LblPriorityExtensionsHeader.Text = lp.GetText("priority.extensions.title");
                            LblPriorityExtensionsDesc.Text = lp.GetText("priority.extensions.desc");
                            LblPriorityExtensionsHint.Text = lp.GetText("priority.extensions.hint");
                            BtnAddPriorityExt.Content = lp.GetText("encryption.add");
                            BtnRemovePriorityExt.Content = lp.GetText("encryption.remove");
                        }
                        catch { }

                        // Update bandwidth settings texts
                        try
                        {
                            LblBandwidthHeader.Text = lp.GetText("bandwidth.header");
                            LblLargeFileThreshold.Text = lp.GetText("bandwidth.threshold.label");
                            TbLargeFileThresholdKb.ToolTip = lp.GetText("bandwidth.threshold.tooltip");
                            LblBandwidthDesc.Text = lp.GetText("bandwidth.threshold.desc");
                        }
                        catch { }

                        // Update settings tab texts
                        try
                        {
                            SettingsSectionTitle.Text = lp.GetText("settings.section.title");
                            SettingsSectionDesc.Text = lp.GetText("settings.section.desc");
                            LblBusinessSectionTitle.Text = lp.GetText("settings.business.title");
                            LblBusinessSectionDesc.Text = lp.GetText("settings.business.desc");
                            LblBusinessStatusHeader.Text = lp.GetText("settings.business.status");
                        }
                        catch { }
                    }
                    catch { }
                }
                catch { }

                // Update job states with translated text
                UpdateJobStatesTranslation();
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
                        System.Windows.MessageBox.Show($"Failed to create job: {err}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            void StartAll()
            {
                // Start all backups in parallel
                List<Task> tasks = new();
                for (int i = 0; i < _backupJobs.Count; i++)
                {
                    int index = i + 1; // Capture index for closure
                    var task = Task.Run(() => StartBackupByIndex(index));
                    tasks.Add(task);
                }

                // Update status text to indicate parallel execution
                var lp = EasySave.Core.LanguageProvider.GetInstance();
                StatusText.Text = lp.GetText("status.all.started") ?? "Tous les travaux sont lancés en parallèle";

                // Optionally wait for all tasks to complete in the background
                _ = Task.WhenAll(tasks).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        StatusText.Text = lp.GetText("status.all.completed") ?? "Tous les travaux sont terminés";
                    });
                });
            }

            void StartSelected()
            {
                // Start selected backups in parallel
                List<Task> tasks = new();
                var selectedItems = JobsList.SelectedItems.Cast<object>().ToList();

                foreach (var item in selectedItems)
                {
                    if (item is JobViewModel vm)
                    {
                        // find matching backup job index
                        int idx = _backupJobs.FindIndex(b => b.GetName() == vm.Name && b.GetSourcePath() == vm.Source && b.GetTargetPath() == vm.Destination);
                        if (idx >= 0)
                        {
                            int index = idx + 1; // Capture index for closure
                            var task = Task.Run(() => StartBackupByIndex(index));
                            tasks.Add(task);
                        }
                    }
                }

                // Update status text to indicate parallel execution
                var lp = EasySave.Core.LanguageProvider.GetInstance();
                StatusText.Text = lp.GetText("status.selected.started") ?? "Travaux sélectionnés lancés en parallèle";

                // Optionally wait for all tasks to complete in the background
                if (tasks.Count > 0)
                {
                    _ = Task.WhenAll(tasks).ContinueWith(_ =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            StatusText.Text = lp.GetText("status.selected.completed") ?? "Travaux sélectionnés terminés";
                        });
                    });
                }
            }

            void StopSelected()
            {
                var selectedItems = JobsList.SelectedItems.Cast<object>().ToList();
                foreach (var item in selectedItems)
                {
                    if (item is JobViewModel vm)
                    {
                        _backupManager.JobController.StopJob(vm.Name);
                    }
                }
            }

            void StopAll()
            {
                foreach (var job in Jobs)
                {
                    _backupManager.JobController.StopJob(job.Name);
                }
            }

            void PauseSelected()
            {
                var selectedItems = JobsList.SelectedItems.Cast<object>().ToList();
                foreach (var item in selectedItems)
                {
                    if (item is JobViewModel vm)
                    {
                        _backupManager.JobController.PauseJob(vm.Name);
                    }
                }
            }

            void PauseAll()
            {
                foreach (var job in Jobs)
                {
                    _backupManager.JobController.PauseJob(job.Name);
                }
            }

            void StartBackupByIndex(int oneBasedIndex)
            {
                // Check if business software is running before starting backup
                if (_backupManager.IsBusinessSoftwareRunning())
                {
                    var runningProcesses = _backupManager.GetRunningMonitoredProcesses();
                    var lpLocal = EasySave.Core.LanguageProvider.GetInstance();

                    string title = lpLocal.GetText("business.error.title");
                    string message = lpLocal.GetText("business.error.message");
                    string instruction = lpLocal.GetText("business.error.instruction");

                    string fullMessage = $"{message}\n\n {string.Join("\n ", runningProcesses)}\n\n{instruction}";

                    System.Windows.MessageBox.Show(fullMessage, title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // set UI state and run in background without progress polling
                var b = _backupJobs.ElementAtOrDefault(oneBasedIndex - 1);
                if (b == null) return;

                // Check if this backup is already running
                string jobKey = $"{b.GetName()}_{b.GetSourcePath()}_{b.GetTargetPath()}";
                lock (_runningTasks)
                {
                    if (_runningTasks.ContainsKey(jobKey))
                    {
                        // Job is already running, skip
                        return;
                    }
                }

                var vm = Jobs.FirstOrDefault(j => j.Name == b.GetName() && j.Source == b.GetSourcePath() && j.Destination == b.GetTargetPath());
                if (vm != null)
                {
                    Dispatcher.Invoke(() => 
                    { 
                        vm.Progress = 0.0;  // Reset progress to 0
                        vm.State = "Running";
                    });
                }

                var task = Task.Run(() =>
                {
                    try
                    {
                        // Ask user whether to encrypt (on UI thread)
                        bool encrypt = false;
                        Dispatcher.Invoke(() =>
                        {
                            var lpLocal = EasySave.Core.LanguageProvider.GetInstance();
                            string prompt = lpLocal.GetText("encryption.prompt.encrypt");
                            string title = lpLocal.GetText("encryption.prompt.title");
                            var res = System.Windows.MessageBox.Show(prompt, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
                            encrypt = (res == MessageBoxResult.Yes);
                        });

                        if (encrypt && string.IsNullOrEmpty(EncryptionKey))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                var lpLocal = EasySave.Core.LanguageProvider.GetInstance();
                                string missing = lpLocal.GetText("encryption.key.missing");
                                string title = lpLocal.GetText("encryption.prompt.title");
                                System.Windows.MessageBox.Show(missing, title, MessageBoxButton.OK, MessageBoxImage.Information);
                            });
                            encrypt = false;
                        }

                        if (!encrypt)
                        {
                            _backupManager.ExecuteBackupJob(oneBasedIndex);
                        }
                        else
                        {
                            // Execute then encrypt copied files in target directory
                            _backupManager.ExecuteBackupJob(oneBasedIndex);
                            try
                            {
                                // find job and encrypt files in target
                                var job = _backupJobs.ElementAtOrDefault(oneBasedIndex - 1);
                                if (job != null)
                                {
                                    // collect extension filters from UI
                                    List<string> extFilters = new();
                                    Dispatcher.Invoke(() =>
                                    {
                                        foreach (var it in LbEncryptionTypes.Items)
                                        {
                                            if (it is string s)
                                            {
                                                var norm = s.Trim();
                                                if (!norm.StartsWith('.')) norm = "." + norm;
                                                extFilters.Add(norm.ToLowerInvariant());
                                            }
                                        }
                                    });

                                    var files = Directory.GetFiles(job.GetTargetPath(), "*", SearchOption.AllDirectories);
                                    foreach (var f in files)
                                    {
                                        try
                                        {
                                            string fileExt = Path.GetExtension(f).ToLowerInvariant();
                                            if (extFilters.Count > 0 && !extFilters.Contains(fileExt))
                                                continue;

                                            // measure encryption time
                                            long encTime = 0;
                                            string tmp = f + ".esenc";
                                            var swEnc = Stopwatch.StartNew();
                                            try
                                            {
                                                CryptoSoft.CryptoService.EncryptFile(f, tmp, EncryptionKey);
                                                swEnc.Stop();
                                                encTime = swEnc.ElapsedMilliseconds;
                                            }
                                            catch
                                            {
                                                try { swEnc.Stop(); } catch { }
                                                encTime = -1; // indicate error
                                            }

                                            try
                                            {
                                                if (encTime > 0 && File.Exists(tmp))
                                                {
                                                    File.Delete(f);
                                                    File.Move(tmp, f);
                                                }
                                                else
                                                {
                                                    if (File.Exists(tmp)) File.Delete(tmp);
                                                }
                                            }
                                            catch { }

                                            // log encryption time for this file (use File entry type)
                                            try
                                            {
                                                long fsize = 0;
                                                try { fsize = new FileInfo(f).Length; } catch { }
                                                var elog = new LogEntry(job.GetName(), f, f, fsize, 0, LogEntry.EntryType.File, encTime);
                                                _logger?.LogAction(elog);
                                            }
                                            catch { }
                                        }
                                        catch { }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                    finally
                    {
                        // Remove from running tasks and update UI state
                        lock (_runningTasks)
                        {
                            _runningTasks.Remove(jobKey);
                        }
                        Dispatcher.Invoke(() => { if (vm != null) vm.State = "Stopped"; });
                    }
                });

                // Store the task in the tracking dictionary
                lock (_runningTasks)
                {
                    _runningTasks[jobKey] = task;
                }
            }
        }

        private void CbLanguage_PreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox cb)
            {
                cb.Focus();
                // Only open the dropdown if it's not already open. Avoid toggling here to prevent
                // closing the dropdown when clicking an item which can prevent SelectionChanged.
                if (!cb.IsDropDownOpen)
                    cb.IsDropDownOpen = true;
             }
        }

        // Individual job control handlers
        private void BtnPlayJob_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string jobName)
            {
                // Find the job and start/resume it
                var job = Jobs.FirstOrDefault(j => j.Name == jobName);
                if (job != null)
                {
                    // Check if paused, if so resume
                    if (_backupManager.JobController.IsPaused(jobName))
                    {
                        _backupManager.JobController.ResumeJob(jobName);
                    }
                    else
                    {
                        // Start the job - find the index and execute via BackupManager
                        var backupJob = _backupJobs.FirstOrDefault(b => b.GetName() == jobName);
                        if (backupJob != null)
                        {
                            int index = _backupJobs.IndexOf(backupJob) + 1;

                            // Check if already running
                            string jobKey = $"{backupJob.GetName()}_{backupJob.GetSourcePath()}_{backupJob.GetTargetPath()}";
                            lock (_runningTasks)
                            {
                                if (_runningTasks.ContainsKey(jobKey))
                                {
                                    return; // Already running
                                }
                            }

                            // Check for business software
                            if (_backupManager.IsBusinessSoftwareRunning())
                            {
                                var runningProcesses = _backupManager.GetRunningMonitoredProcesses();
                                var lp = EasySave.Core.LanguageProvider.GetInstance();
                                string title = lp.GetText("business.error.title");
                                string message = lp.GetText("business.error.message");
                                string instruction = lp.GetText("business.error.instruction");
                                string fullMessage = $"{message}\n\n {string.Join("\n ", runningProcesses)}\n\n{instruction}";
                                System.Windows.MessageBox.Show(fullMessage, title, MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            // Reset progress to 0
                            job.Progress = 0.0;

                            // Start the backup task
                            var task = Task.Run(() =>
                            {
                                try
                                {
                                    // Ask user whether to encrypt (on UI thread)
                                    bool encrypt = false;
                                    Dispatcher.Invoke(() =>
                                    {
                                        var lpLocal = EasySave.Core.LanguageProvider.GetInstance();
                                        string prompt = lpLocal.GetText("encryption.prompt.encrypt");
                                        string title = lpLocal.GetText("encryption.prompt.title");
                                        var res = System.Windows.MessageBox.Show(prompt, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
                                        encrypt = (res == MessageBoxResult.Yes);
                                    });

                                    if (encrypt && string.IsNullOrEmpty(EncryptionKey))
                                    {
                                        Dispatcher.Invoke(() =>
                                        {
                                            var lpLocal = EasySave.Core.LanguageProvider.GetInstance();
                                            string missing = lpLocal.GetText("encryption.key.missing");
                                            string title = lpLocal.GetText("encryption.prompt.title");
                                            System.Windows.MessageBox.Show(missing, title, MessageBoxButton.OK, MessageBoxImage.Information);
                                        });
                                        encrypt = false;
                                    }

                                    Dispatcher.Invoke(() => { if (job != null) job.State = "Running"; });

                                    if (!encrypt)
                                    {
                                        _backupManager.ExecuteBackupJob(index);
                                    }
                                    else
                                    {
                                        // Execute then encrypt copied files in target directory
                                        _backupManager.ExecuteBackupJob(index);
                                        try
                                        {
                                            // collect extension filters from UI
                                            List<string> extFilters = new();
                                            Dispatcher.Invoke(() =>
                                            {
                                                foreach (var it in LbEncryptionTypes.Items)
                                                {
                                                    if (it is string s)
                                                    {
                                                        var norm = s.Trim();
                                                        if (!norm.StartsWith('.')) norm = "." + norm;
                                                        extFilters.Add(norm.ToLowerInvariant());
                                                    }
                                                }
                                            });

                                            var files = Directory.GetFiles(backupJob.GetTargetPath(), "*", SearchOption.AllDirectories);
                                            foreach (var f in files)
                                            {
                                                try
                                                {
                                                    string fileExt = Path.GetExtension(f).ToLowerInvariant();
                                                    if (extFilters.Count > 0 && !extFilters.Contains(fileExt))
                                                        continue;

                                                    // measure encryption time
                                                    long encTime = 0;
                                                    string tmp = f + ".esenc";
                                                    var swEnc = Stopwatch.StartNew();
                                                    try
                                                    {
                                                        CryptoSoft.CryptoService.EncryptFile(f, tmp, EncryptionKey);
                                                        swEnc.Stop();
                                                        encTime = swEnc.ElapsedMilliseconds;
                                                    }
                                                    catch
                                                    {
                                                        try { swEnc.Stop(); } catch { }
                                                        encTime = -1; // indicate error
                                                    }

                                                    try
                                                    {
                                                        if (encTime > 0 && File.Exists(tmp))
                                                        {
                                                            File.Delete(f);
                                                            File.Move(tmp, f);
                                                        }
                                                        else
                                                        {
                                                            if (File.Exists(tmp)) File.Delete(tmp);
                                                        }
                                                    }
                                                    catch { }

                                                    // log encryption time for this file
                                                    try
                                                    {
                                                        long fsize = 0;
                                                        try { fsize = new FileInfo(f).Length; } catch { }
                                                        var elog = new LogEntry(backupJob.GetName(), f, f, fsize, 0, LogEntry.EntryType.File, encTime);
                                                        _logger?.LogAction(elog);
                                                    }
                                                    catch { }
                                                }
                                                catch { }
                                            }
                                        }
                                        catch { }
                                    }
                                }
                                catch { }
                                finally
                                {
                                    lock (_runningTasks)
                                    {
                                        _runningTasks.Remove(jobKey);
                                    }
                                }
                            });

                            lock (_runningTasks)
                            {
                                _runningTasks[jobKey] = task;
                            }
                        }
                    }
                }
            }
        }

        private void BtnPauseJob_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string jobName)
            {
                _backupManager.JobController.PauseJob(jobName);
            }
        }

        private void BtnStopJob_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string jobName)
            {
                _backupManager.JobController.StopJob(jobName);
            }
        }

        private void BtnDecryptJob_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button btn || btn.Tag is not string jobName)
                return;

            var backupJob = _backupJobs.FirstOrDefault(b => b.GetName() == jobName);
            if (backupJob == null) return;

            var dialog = new DecryptPasswordDialog { Owner = this };
            if (dialog.ShowDialog() != true)
                return;

            string password = dialog.Password;

            List<string> extFilters = new();
            foreach (var it in LbEncryptionTypes.Items)
            {
                if (it is string s)
                {
                    var norm = s.Trim();
                    if (!norm.StartsWith('.')) norm = "." + norm;
                    extFilters.Add(norm.ToLowerInvariant());
                }
            }

            _ = Task.Run(() =>
            {
                int successCount = 0, errorCount = 0;
                try
                {
                    var files = Directory.GetFiles(backupJob.GetTargetPath(), "*", SearchOption.AllDirectories);
                    foreach (var f in files)
                    {
                        try
                        {
                            string fileExt = Path.GetExtension(f).ToLowerInvariant();
                            if (extFilters.Count > 0 && !extFilters.Contains(fileExt))
                                continue;

                            string tmp = f + ".esdec";
                            CryptoSoft.CryptoService.DecryptFile(f, tmp, password);
                            File.Delete(f);
                            File.Move(tmp, f);
                            successCount++;
                        }
                        catch
                        {
                            errorCount++;
                            try { if (File.Exists(f + ".esdec")) File.Delete(f + ".esdec"); } catch { }
                        }
                    }
                }
                catch { }

                Dispatcher.Invoke(() =>
                {
                    var lp = EasySave.Core.LanguageProvider.GetInstance();
                    if (errorCount == 0)
                    {
                        System.Windows.MessageBox.Show(
                            string.Format(lp.GetText("decrypt.success"), successCount),
                            lp.GetText("decrypt.prompt.title"),
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(
                            string.Format(lp.GetText("decrypt.partial"), successCount, errorCount),
                            lp.GetText("decrypt.prompt.title"),
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                });
            });
        }

        // Progress update timer handler
        private void UpdateJobsProgress(object? sender, EventArgs e)
        {
            foreach (var job in Jobs)
            {
                var state = _stateManager.GetJobState(job.Name);
                if (state != null)
                {
                    // Update progress
                    job.Progress = state.Progress;

                    // Update state with translation
                    var lp = EasySave.Core.LanguageProvider.GetInstance();
                    string translatedState = state.Status switch
                    {
                        StateStatus.Active => lp.GetText("state.running"),
                        StateStatus.Paused => lp.GetText("state.paused"),
                        StateStatus.Inactive => lp.GetText("state.stopped"),
                        StateStatus.Completed => lp.GetText("state.completed"),
                        StateStatus.Error => lp.GetText("state.error"),
                        _ => lp.GetText("state.inactive")
                    };

                    job.State = translatedState;
                }
            }
        }

        // Update job states with current language
        private void UpdateJobStatesTranslation()
        {
            foreach (var job in Jobs)
            {
                var state = _stateManager.GetJobState(job.Name);
                if (state != null)
                {
                    var lp = EasySave.Core.LanguageProvider.GetInstance();
                    string translatedState = state.Status switch
                    {
                        StateStatus.Active => lp.GetText("state.running"),
                        StateStatus.Paused => lp.GetText("state.paused"),
                        StateStatus.Inactive => lp.GetText("state.stopped"),
                        StateStatus.Completed => lp.GetText("state.completed"),
                        StateStatus.Error => lp.GetText("state.error"),
                        _ => lp.GetText("state.inactive")
                    };

                    job.State = translatedState;
                }
            }
        }

        private void LoadBusinessProcesses()
        {
            LbBusinessProcesses.Items.Clear();
            var processes = _backupManager.GetMonitoredProcesses();
            foreach (var process in processes)
            {
                LbBusinessProcesses.Items.Add(process);
            }
        }

        private void LoadPriorityExtensions()
        {
            LbPriorityExtensions.Items.Clear();
            var extensions = _backupManager.GetPriorityExtensions();
            foreach (var ext in extensions)
            {
                LbPriorityExtensions.Items.Add(ext);
            }
        }

        private void UpdateBusinessStatus()
        {
            bool isRunning = _backupManager.IsBusinessSoftwareRunning();

            if (isRunning)
            {
                var runningProcesses = _backupManager.GetRunningMonitoredProcesses();
                string processNames = string.Join(", ", runningProcesses);

                // Update to error state
                BusinessStatusBorder.Background = (System.Windows.Media.Brush)FindResource("DangerSoft");
                BusinessStatusBorder.BorderBrush = (System.Windows.Media.Brush)FindResource("DangerColor");
                TbBusinessStatus.Text = $"?? Logiciels métiers détectés : {processNames}";
                TbBusinessStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xDC, 0x26, 0x26));
                TbBusinessStatusDetail.Text = "Les sauvegardes sont bloquées";
                TbBusinessStatusDetail.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xB9, 0x1C, 0x1C));
            }
            else
            {
                // Update to success state
                BusinessStatusBorder.Background = (System.Windows.Media.Brush)FindResource("SuccessSoft");
                BusinessStatusBorder.BorderBrush = (System.Windows.Media.Brush)FindResource("SuccessColor");
                TbBusinessStatus.Text = "? Aucun logiciel métier détecté";
                TbBusinessStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x15, 0x80, 0x3D));
                TbBusinessStatusDetail.Text = "Les sauvegardes sont autorisées";
                TbBusinessStatusDetail.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x4D, 0x7C, 0x0F));
            }
        }

        private void LoadRemoteLoggingConfig()
        {
            try
            {
                var config = AppConfig.Instance;

                CbEnableRemoteLogging.IsChecked = config.RemoteLogging.Enabled;
                TbRemoteLoggingHost.Text = config.RemoteLogging.ServerHost;
                TbRemoteLoggingPort.Text = config.RemoteLogging.ServerPort.ToString();
                CbKeepLocalLogs.IsChecked = config.RemoteLogging.EnableLocalLogging;

                // Update panel state
                if (config.RemoteLogging.Enabled)
                {
                    PnlRemoteLoggingSettings.IsEnabled = true;
                    PnlRemoteLoggingSettings.Opacity = 1.0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load remote logging config: {ex.Message}");
            }
        }

        private void SaveRemoteLoggingConfig()
        {
            try
            {
                var config = AppConfig.Instance;

                config.RemoteLogging.Enabled = CbEnableRemoteLogging.IsChecked == true;
                config.RemoteLogging.ServerHost = TbRemoteLoggingHost.Text?.Trim() ?? "localhost";

                if (int.TryParse(TbRemoteLoggingPort.Text, out int port))
                {
                    config.RemoteLogging.ServerPort = port;
                }

                config.RemoteLogging.EnableLocalLogging = CbKeepLocalLogs.IsChecked == true;

                config.SaveConfig();

                ShowRemoteLoggingStatus("✓ Configuration enregistrée avec succès", true);
            }
            catch (Exception ex)
            {
                ShowRemoteLoggingStatus($"✗ Erreur : {ex.Message}", false);
            }
        }

        private async Task TestRemoteLoggingConnection()
        {
            try
            {
                string host = TbRemoteLoggingHost.Text?.Trim();
                if (string.IsNullOrWhiteSpace(host))
                {
                    ShowRemoteLoggingStatus("✗ Veuillez entrer une adresse serveur", false);
                    return;
                }

                if (!int.TryParse(TbRemoteLoggingPort.Text, out int port))
                {
                    ShowRemoteLoggingStatus("✗ Port invalide", false);
                    return;
                }

                ShowRemoteLoggingStatus("🔄 Test de connexion en cours...", null);

                // Test connection
                await Task.Run(() =>
                {
                    using var client = new System.Net.Sockets.TcpClient();
                    var connectTask = client.ConnectAsync(host, port);
                    if (!connectTask.Wait(TimeSpan.FromSeconds(5)))
                    {
                        throw new TimeoutException("Timeout de connexion");
                    }
                });

                ShowRemoteLoggingStatus($"✓ Connexion réussie à {host}:{port}", true);

                // Auto-save on successful test
                SaveRemoteLoggingConfig();
            }
            catch (Exception ex)
            {
                ShowRemoteLoggingStatus($"✗ Connexion échouée : {ex.Message}", false);
            }
        }

        private void ShowRemoteLoggingStatus(string message, bool? success)
        {
            RemoteLoggingStatusBorder.Visibility = Visibility.Visible;
            TbRemoteLoggingStatus.Text = message;

            if (success.HasValue)
            {
                if (success.Value)
                {
                    RemoteLoggingStatusBorder.Background = (System.Windows.Media.Brush)FindResource("SuccessSoft");
                    RemoteLoggingStatusBorder.BorderBrush = (System.Windows.Media.Brush)FindResource("SuccessColor");
                    TbRemoteLoggingStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0x15, 0x80, 0x3D));
                }
                else
                {
                    RemoteLoggingStatusBorder.Background = (System.Windows.Media.Brush)FindResource("DangerSoft");
                    RemoteLoggingStatusBorder.BorderBrush = (System.Windows.Media.Brush)FindResource("DangerColor");
                    TbRemoteLoggingStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0xDC, 0x26, 0x26));
                }
            }
            else
            {
                // Neutral (loading state)
                RemoteLoggingStatusBorder.Background = (System.Windows.Media.Brush)FindResource("NeutralSoft");
                RemoteLoggingStatusBorder.BorderBrush = (System.Windows.Media.Brush)FindResource("BorderColor");
                TbRemoteLoggingStatus.Foreground = (System.Windows.Media.Brush)FindResource("TextSecondary");
            }
        }

        // Input validation for large-file threshold textbox
        private void TbLargeFileThresholdKb_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c))
                {
                    e.Handled = true;
                    return;
                }
            }
            e.Handled = false;
        }

        private void TbLargeFileThresholdKb_Pasting(object sender, System.Windows.DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!int.TryParse(text, out _))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}
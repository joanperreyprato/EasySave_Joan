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
                StatusText.Text = lp.GetText("status.ready");
            // update column headers
            try
            {
                ColNameHeader.Text = lp.GetText("col.name");
                ColSourceHeader.Text = lp.GetText("col.source");
                ColTargetHeader.Text = lp.GetText("col.target");
                ColStateHeader.Text = lp.GetText("col.state");
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
                }
                catch { }
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
                // Check if business software is running before starting backup
                if (_backupManager.IsBusinessSoftwareRunning())
                {
                    var runningProcesses = _backupManager.GetRunningMonitoredProcesses();
                    var lpLocal = EasySave.Core.LanguageProvider.GetInstance();

                    string title = lpLocal.GetText("business.error.title");
                    string message = lpLocal.GetText("business.error.message");
                    string instruction = lpLocal.GetText("business.error.instruction");

                    string fullMessage = $"{message}\n\n• {string.Join("\n• ", runningProcesses)}\n\n{instruction}";

                    MessageBox.Show(fullMessage, title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // set UI state and run in background without progress polling
                var b = _backupJobs.ElementAtOrDefault(oneBasedIndex - 1);
                if (b == null) return;

                var vm = Jobs.FirstOrDefault(j => j.Name == b.GetName() && j.Source == b.GetSourcePath() && j.Destination == b.GetTargetPath());
                if (vm != null) vm.State = "Running";

                _ = Task.Run(() =>
                {
                    try
                    {
                        // Ask user whether to encrypt (on UI thread)
                        bool encrypt = false;
                        Dispatcher.Invoke(() => {
                            var lpLocal = EasySave.Core.LanguageProvider.GetInstance();
                            string prompt = lpLocal.GetText("encryption.prompt.encrypt");
                            string title = lpLocal.GetText("encryption.prompt.title");
                            var res = MessageBox.Show(prompt, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
                            encrypt = (res == MessageBoxResult.Yes);
                        });

                        if (encrypt && string.IsNullOrEmpty(EncryptionKey))
                        {
                            Dispatcher.Invoke(() => {
                                var lpLocal = EasySave.Core.LanguageProvider.GetInstance();
                                string missing = lpLocal.GetText("encryption.key.missing");
                                string title = lpLocal.GetText("encryption.prompt.title");
                                MessageBox.Show(missing, title, MessageBoxButton.OK, MessageBoxImage.Information);
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
                        Dispatcher.Invoke(() => { if (vm != null) vm.State = "Stopped"; });
                    }
                });
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
    }
}
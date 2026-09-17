using EasySave.Core.Services;
using EasySave.WPF.ViewModels;

namespace EasySave.WPF;

/// <summary>
/// Application entry point. Bootstraps all services and wires the MainViewModel.
/// </summary>
public partial class App : System.Windows.Application
{
    /// <inheritdoc/>
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        // Bootstrap services (manual DI — no container needed for this scale)
        var stateManager = new StateManager();
        var appSettings = new AppSettings();
        var monitor = new BusinessSoftwareMonitor
        {
            ProcessName = appSettings.BusinessSoftwareName
        };
        var backupService = new BackupServiceV3(stateManager, appSettings, monitor);
        var viewModel = new MainViewModel(backupService, appSettings, monitor, stateManager);

        // Apply light theme on startup
        ThemeManager.Apply(isDark: false);

        var mainWindow = new Views.MainWindow(viewModel);
        mainWindow.Show();
    }
}

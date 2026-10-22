using EasySave.Core.Models;
using EasySave.Core.Services;
using EasySave.WPF.ViewModels;
using System.IO;
using Xunit;

namespace EasySave.Tests.WPF;

/// <summary>
/// Component tests for the WPF ViewModels.
/// Tests UI data-binding properties and command logic without a real window.
/// </summary>
public sealed class MainViewModelTests : IDisposable
{
    private readonly MainViewModel _viewModel;
    private readonly string _statePath;
    private readonly string _cfgPath;
    private readonly string _appSetPath;
    private readonly string _src;
    private readonly string _tgt;

    public MainViewModelTests()
    {
        _statePath  = Path.Combine(Path.GetTempPath(), $"es_state_{Guid.NewGuid():N}.json");
        _cfgPath    = Path.Combine(Path.GetTempPath(), $"es_cfg_{Guid.NewGuid():N}.json");
        _appSetPath = Path.Combine(Path.GetTempPath(), $"es_app_{Guid.NewGuid():N}.json");
        _src        = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"es_src_{Guid.NewGuid():N}")).FullName;
        _tgt        = Path.Combine(Path.GetTempPath(), $"es_tgt_{Guid.NewGuid():N}");

        var stateManager = new StateManager(_statePath);
        var settings     = new AppSettings(_appSetPath);
        var monitor      = new BusinessSoftwareMonitor();
        var backupSvc    = new BackupServiceV3(stateManager, settings, monitor, _cfgPath);

        _viewModel = new MainViewModel(backupSvc, settings, monitor, stateManager);
    }

    public void Dispose()
    {
        if (File.Exists(_statePath)) File.Delete(_statePath);
        if (File.Exists(_cfgPath)) File.Delete(_cfgPath);
        if (File.Exists(_appSetPath)) File.Delete(_appSetPath);
        if (Directory.Exists(_src)) Directory.Delete(_src, recursive: true);
    }

    [Fact]
    public void CreateJob_AddsJobToObservableCollection()
    {
        int initialCount = _viewModel.Jobs.Count;

        var result = _viewModel.CreateJob("TestJob", _src, _tgt, BackupType.Full);

        Assert.Equal(BackupCreationResult.Success, result);
        Assert.Equal(initialCount + 1, _viewModel.Jobs.Count);
        Assert.Equal("TestJob", _viewModel.Jobs[^1].Name);
    }

    [Fact]
    public void DeleteJob_RemovesJobFromObservableCollection()
    {
        _viewModel.CreateJob("ToDelete", _src, _tgt, BackupType.Full);
        _viewModel.SelectedJob = _viewModel.Jobs.First(j => j.Name == "ToDelete");

        Assert.True(_viewModel.DeleteJobCommand.CanExecute(null));
        _viewModel.DeleteJobCommand.Execute(null);

        Assert.DoesNotContain(_viewModel.Jobs, j => j.Name == "ToDelete");
    }

    [Fact]
    public void ToggleLanguage_SwitchesBetweenEnAndFr()
    {
        Assert.Equal("EN", _viewModel.CurrentLanguageText);

        _viewModel.ToggleLanguageCommand.Execute(null);
        Assert.Equal("FR", _viewModel.CurrentLanguageText);

        _viewModel.ToggleLanguageCommand.Execute(null);
        Assert.Equal("EN", _viewModel.CurrentLanguageText);
    }

    [Fact]
    public void SaveSettings_UpdatesServices()
    {
        _viewModel.BusinessSoftwareName = "notepad";
        _viewModel.EncryptionExtensions = ".txt, .pdf";
        
        _viewModel.SaveSettingsCommand.Execute(null);

        Assert.Equal("✓ Settings saved.", _viewModel.StatusMessage);
    }
}

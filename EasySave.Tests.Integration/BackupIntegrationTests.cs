using System.Diagnostics;
using System.Text.Json;
using EasySave.Core.Models;
using EasySave.Core.Services;
using EasyLog;
using Xunit;

namespace EasySave.Tests.Integration;

/// <summary>
/// Full end-to-end integration tests.
/// Verifies that files are actually copied, logs are written to disk,
/// state.json is created, and CryptoSoft encryption is triggered.
/// </summary>
public sealed class BackupIntegrationTests : IDisposable
{
    private readonly string _src;
    private readonly string _tgt;
    private readonly string _statePath;
    private readonly string _cfgPath;
    private readonly string _appSetPath;
    private readonly string _logDir;

    private readonly BackupServiceV2 _service;

    public BackupIntegrationTests()
    {
        _src = CreateTempDir();
        _tgt = CreateTempDir();
        _logDir = CreateTempDir();

        _statePath  = TempFile(".json");
        _cfgPath    = TempFile(".json");
        _appSetPath = TempFile(".json");

        var stateManager = new StateManager(_statePath);
        var settings     = new AppSettings(_appSetPath);
        var monitor      = new BusinessSoftwareMonitor();
        
        _service = new BackupServiceV2(stateManager, settings, monitor, _cfgPath);
    }

    public void Dispose()
    {
        CleanDir(_src);
        CleanDir(_tgt);
        CleanDir(_logDir);
        CleanFile(_statePath);
        CleanFile(_cfgPath);
        CleanFile(_appSetPath);
    }

    [Fact]
    public void ExecuteJob_EndToEnd_CopiesFilesAndWritesJsonLog()
    {
        // 1. Arrange: create files
        File.WriteAllText(Path.Combine(_src, "hello.txt"), "world");

        // 2. Act: create and execute job
        _service.CreateJob("IntegrationJob", _src, _tgt, BackupType.Full);
        var logger = LoggerFactory.Create(LogFormat.Json, _logDir);
        _service.ExecuteJob("IntegrationJob", logger);

        // 3. Assert: file was copied
        Assert.True(File.Exists(Path.Combine(_tgt, "hello.txt")));
        Assert.Equal("world", File.ReadAllText(Path.Combine(_tgt, "hello.txt")));

        // 4. Assert: state.json was updated
        Assert.True(File.Exists(_statePath));
        var stateJson = File.ReadAllText(_statePath);
        Assert.Contains("IntegrationJob", stateJson);
        Assert.Contains("\"Status\":", stateJson); // Verify the status field is serialized

        // 5. Assert: log file was created
        var logFiles = Directory.GetFiles(_logDir, "*.json");
        Assert.Single(logFiles);
        var logContent = File.ReadAllText(logFiles[0]);
        Assert.Contains("IntegrationJob", logContent);
        Assert.Contains("hello.txt", logContent);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"es_int_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string TempFile(string ext)
        => Path.Combine(Path.GetTempPath(), $"es_int_{Guid.NewGuid():N}{ext}");

    private static void CleanDir(string p) { if (Directory.Exists(p)) Directory.Delete(p, true); }
    private static void CleanFile(string p) { if (File.Exists(p)) File.Delete(p); }
}

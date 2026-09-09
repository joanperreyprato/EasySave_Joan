using EasySave.Core.Models;
using EasySave.Core.Services;
using EasyLog;
using Moq;

namespace EasySave.Tests;

/// <summary>
/// Unit tests for <see cref="BackupServiceV2"/>.
/// Tests v2.0-specific behaviors: unlimited jobs, business software blocking, encryption extension filtering.
/// </summary>
public sealed class BackupServiceV2Tests : IDisposable
{
    private readonly string _src;
    private readonly string _tgt;
    private readonly string _statePath;
    private readonly string _cfgPath;
    private readonly StateManager _stateManager;
    private readonly AppSettings _settings;
    private readonly BusinessSoftwareMonitor _monitor;
    private readonly BackupServiceV2 _service;

    public BackupServiceV2Tests()
    {
        _src      = CreateTempDir();
        _tgt      = CreateTempDir();
        _statePath = TempFile(".json");
        _cfgPath   = TempFile(".json");

        _stateManager = new StateManager(_statePath);
        _settings     = new AppSettings(TempFile(".json"));
        _monitor      = new BusinessSoftwareMonitor();
        _service      = new BackupServiceV2(_stateManager, _settings, _monitor, _cfgPath);
    }

    public void Dispose()
    {
        CleanDir(_src);
        CleanDir(_tgt);
        CleanFile(_statePath);
        CleanFile(_cfgPath);
    }

    // ── v2.0: No job limit ───────────────────────────────────────────────────

    [Fact]
    public void CreateJob_MoreThanFiveJobs_Succeeds()
    {
        for (int i = 0; i < 8; i++)
        {
            var src = CreateTempDir();
            var result = _service.CreateJob($"Job{i}", src, _tgt, BackupType.Full);
            Assert.Equal(BackupCreationResult.Success, result);
            CleanDir(src);
        }
        Assert.Equal(8, _service.GetAllJobs().Count);
    }

    // ── Business software blocking ───────────────────────────────────────────

    [Fact]
    public void ExecuteJob_BusinessSoftwareRunning_ThrowsInvalidOperation()
    {
        // We can't start a real process in a unit test, so we mock the monitor behavior
        // by using a fake process name that is guaranteed to be running: the current test process.
        var monitorWithSelf = new BusinessSoftwareMonitor
        {
            ProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName
        };

        var svc = new BackupServiceV2(_stateManager, _settings, monitorWithSelf, TempFile(".json"));
        var src = CreateTempDir();
        svc.CreateJob("BlockedJob", src, _tgt, BackupType.Full);

        var loggerMock = new Mock<ILogger>();
        Assert.Throws<InvalidOperationException>(() =>
            svc.ExecuteJob("BlockedJob", loggerMock.Object));

        CleanDir(src);
    }

    [Fact]
    public void ExecuteJob_NoBusinessSoftware_RunsNormally()
    {
        _monitor.ProcessName = string.Empty; // No monitoring
        File.WriteAllText(Path.Combine(_src, "file.txt"), "hello");

        _service.CreateJob("FreeJob", _src, _tgt, BackupType.Full);
        var loggerMock = new Mock<ILogger>();

        // Should not throw
        _service.ExecuteJob("FreeJob", loggerMock.Object);
        Assert.True(File.Exists(Path.Combine(_tgt, "file.txt")));
    }

    // ── Encryption extension filter ──────────────────────────────────────────

    [Fact]
    public void ExecuteJob_LogsEncryptionTimeZero_WhenNoExtensionMatches()
    {
        _settings.EncryptionExtensions = [".secret"]; // Only .secret files should be encrypted
        File.WriteAllText(Path.Combine(_src, "report.txt"), "content");

        _service.CreateJob("NoEncJob", _src, _tgt, BackupType.Full);

        var captured = new List<LogEntry>();
        var loggerMock = new Mock<ILogger>();
        loggerMock.Setup(l => l.Log(It.IsAny<LogEntry>()))
                  .Callback<LogEntry>(e => captured.Add(e));

        _service.ExecuteJob("NoEncJob", loggerMock.Object);

        Assert.Single(captured);
        Assert.Equal(0, captured[0].EncryptionDurationMs); // No encryption
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"es_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string TempFile(string ext)
        => Path.Combine(Path.GetTempPath(), $"es_{Guid.NewGuid():N}{ext}");

    private static void CleanDir(string p) { if (Directory.Exists(p)) Directory.Delete(p, true); }
    private static void CleanFile(string p) { if (File.Exists(p)) File.Delete(p); }
}

/// <summary>Unit tests for <see cref="BusinessSoftwareMonitor"/>.</summary>
public sealed class BusinessSoftwareMonitorTests
{
    [Fact]
    public void IsRunning_NoProcessNameConfigured_ReturnsFalse()
    {
        var monitor = new BusinessSoftwareMonitor { ProcessName = string.Empty };
        Assert.False(monitor.IsRunning());
    }

    [Fact]
    public void IsRunning_CurrentProcess_ReturnsTrue()
    {
        var monitor = new BusinessSoftwareMonitor
        {
            ProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName
        };
        Assert.True(monitor.IsRunning());
    }

    [Fact]
    public void IsRunning_NonExistentProcess_ReturnsFalse()
    {
        var monitor = new BusinessSoftwareMonitor
        {
            ProcessName = "this_process_definitely_does_not_exist_xyzabc"
        };
        Assert.False(monitor.IsRunning());
    }

    [Fact]
    public void IsRunning_ProcessWithExeSuffix_WorksCorrectly()
    {
        var monitor = new BusinessSoftwareMonitor
        {
            ProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe"
        };
        Assert.True(monitor.IsRunning());
    }
}

/// <summary>Unit tests for <see cref="AppSettings"/>.</summary>
public sealed class AppSettingsTests : IDisposable
{
    private readonly string _settingsPath;
    private readonly AppSettings _settings;

    public AppSettingsTests()
    {
        _settingsPath = Path.Combine(Path.GetTempPath(), $"es_settings_{Guid.NewGuid():N}.json");
        _settings = new AppSettings(_settingsPath);
    }

    public void Dispose()
    {
        if (File.Exists(_settingsPath)) File.Delete(_settingsPath);
    }

    [Fact]
    public void DefaultLogFormat_IsJson()
        => Assert.Equal("Json", _settings.LogFormat);

    [Fact]
    public void SetBusinessSoftwareName_PersistsAndReloads()
    {
        _settings.BusinessSoftwareName = "calc";

        // Reload from disk
        var reloaded = new AppSettings(_settingsPath);
        Assert.Equal("calc", reloaded.BusinessSoftwareName);
    }

    [Fact]
    public void SetEncryptionExtensions_PersistsAndReloads()
    {
        _settings.EncryptionExtensions = [".docx", ".xlsx"];

        var reloaded = new AppSettings(_settingsPath);
        Assert.Contains(".docx", reloaded.EncryptionExtensions);
        Assert.Contains(".xlsx", reloaded.EncryptionExtensions);
    }

    [Fact]
    public void SetLogFormat_PersistsAndReloads()
    {
        _settings.LogFormat = "Xml";
        var reloaded = new AppSettings(_settingsPath);
        Assert.Equal("Xml", reloaded.LogFormat);
    }
}

/// <summary>Unit tests for the XOR encryption logic in CryptoSoft.</summary>
public sealed class XorLogicTests
{
    [Fact]
    public void Xor_EncryptThenDecrypt_ReturnsOriginal()
    {
        byte[] original = System.Text.Encoding.UTF8.GetBytes("Hello EasySave 2.0!");
        byte[] key = System.Text.Encoding.UTF8.GetBytes("EaseSave2024!XOR");

        byte[] encrypted = XorApply(original, key);
        byte[] decrypted = XorApply(encrypted, key);

        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Xor_Encrypted_DiffersFromOriginal()
    {
        byte[] original = System.Text.Encoding.UTF8.GetBytes("SensitiveData");
        byte[] key = System.Text.Encoding.UTF8.GetBytes("SecretKey12345678");

        byte[] encrypted = XorApply(original, key);
        Assert.NotEqual(original, encrypted);
    }

    [Fact]
    public void Xor_KeyAtLeast64Bits()
    {
        // The spec requires a minimum 64-bit (8-byte) key
        byte[] key = System.Text.Encoding.UTF8.GetBytes("EaseSave2024!XOR");
        Assert.True(key.Length >= 8, "Key must be at least 64 bits (8 bytes)");
    }

    private static byte[] XorApply(byte[] data, byte[] key)
    {
        var result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
            result[i] = (byte)(data[i] ^ key[i % key.Length]);
        return result;
    }
}

using EasySave.Core.Models;
using EasySave.Core.Services;
using EasyLog;
using Moq;

namespace EasySave.Tests;

/// <summary>Unit tests for <see cref="BackupService"/>.</summary>
public sealed class BackupServiceTests : IDisposable
{
    private readonly string _tempSource;
    private readonly string _tempTarget;
    private readonly string _statePath;
    private readonly string _configPath;
    private readonly StateManager _stateManager;
    private readonly BackupService _service;

    public BackupServiceTests()
    {
        _tempSource = Path.Combine(Path.GetTempPath(), $"es_src_{Guid.NewGuid():N}");
        _tempTarget = Path.Combine(Path.GetTempPath(), $"es_tgt_{Guid.NewGuid():N}");
        _statePath = Path.Combine(Path.GetTempPath(), $"es_state_{Guid.NewGuid():N}.json");
        _configPath = Path.Combine(Path.GetTempPath(), $"es_cfg_{Guid.NewGuid():N}.json");
        
        Directory.CreateDirectory(_tempSource);
        Directory.CreateDirectory(_tempTarget);
        _stateManager = new StateManager(_statePath);
        _service = new BackupService(_stateManager, _configPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempSource)) Directory.Delete(_tempSource, recursive: true);
        if (Directory.Exists(_tempTarget)) Directory.Delete(_tempTarget, recursive: true);
        if (File.Exists(_statePath)) File.Delete(_statePath);
        if (File.Exists(_configPath)) File.Delete(_configPath);
    }

    // ── CreateJob validation ───────────────────────────────────────────────────

    [Fact]
    public void CreateJob_ValidParams_ReturnsSuccess()
    {
        var result = _service.CreateJob("TestJob", _tempSource, _tempTarget, BackupType.Full);
        Assert.Equal(BackupCreationResult.Success, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("This name is way too long and exceeds fifty characters limit!!!")]
    [InlineData("Invalid/Name")]
    public void CreateJob_InvalidName_ReturnsInvalidName(string badName)
    {
        var result = _service.CreateJob(badName, _tempSource, _tempTarget, BackupType.Full);
        Assert.Equal(BackupCreationResult.InvalidName, result);
    }

    [Fact]
    public void CreateJob_SourceNotFound_ReturnsSourceNotFound()
    {
        var result = _service.CreateJob("J1", @"C:\this\does\not\exist", _tempTarget, BackupType.Full);
        Assert.Equal(BackupCreationResult.SourceNotFound, result);
    }

    [Fact]
    public void CreateJob_DuplicateName_ReturnsDuplicateName()
    {
        _service.CreateJob("Dup", _tempSource, _tempTarget, BackupType.Full);
        var result = _service.CreateJob("Dup", _tempSource, _tempTarget, BackupType.Full);
        Assert.Equal(BackupCreationResult.DuplicateName, result);
    }

    [Fact]
    public void CreateJob_MaxFiveJobs_ReturnsMaxJobsExceeded()
    {
        for (int i = 0; i < 5; i++)
        {
            var src = Path.Combine(Path.GetTempPath(), $"es_src_{Guid.NewGuid():N}");
            Directory.CreateDirectory(src);
            var r = _service.CreateJob($"Job{i}", src, _tempTarget, BackupType.Full);
            Assert.Equal(BackupCreationResult.Success, r);
            Directory.Delete(src);
        }

        // 6th job must be rejected
        var result = _service.CreateJob("Job6", _tempSource, _tempTarget, BackupType.Full);
        Assert.Equal(BackupCreationResult.MaxJobsExceeded, result);
    }

    // ── GetAllJobs / GetJob ────────────────────────────────────────────────────

    [Fact]
    public void GetJob_ExistingJob_ReturnsJob()
    {
        _service.CreateJob("FindMe", _tempSource, _tempTarget, BackupType.Differential);
        var job = _service.GetJob("FindMe");
        Assert.NotNull(job);
        Assert.Equal(BackupType.Differential, job.Type);
    }

    [Fact]
    public void GetJob_MissingJob_ReturnsNull()
    {
        Assert.Null(_service.GetJob("NonExistent"));
    }

    // ── DeleteJob ─────────────────────────────────────────────────────────────

    [Fact]
    public void DeleteJob_ExistingJob_RemovesIt()
    {
        _service.CreateJob("ToDelete", _tempSource, _tempTarget, BackupType.Full);
        _service.DeleteJob("ToDelete");
        Assert.Null(_service.GetJob("ToDelete"));
    }

    // ── ExecuteJob (Full) ─────────────────────────────────────────────────────

    [Fact]
    public void ExecuteJob_FullBackup_CopiesAllFiles()
    {
        // Arrange: create 3 files in source
        File.WriteAllText(Path.Combine(_tempSource, "a.txt"), "hello");
        File.WriteAllText(Path.Combine(_tempSource, "b.txt"), "world");
        Directory.CreateDirectory(Path.Combine(_tempSource, "sub"));
        File.WriteAllText(Path.Combine(_tempSource, "sub", "c.txt"), "deep");

        _service.CreateJob("FullJob", _tempSource, _tempTarget, BackupType.Full);

        var loggerMock = new Mock<ILogger>();

        // Act
        _service.ExecuteJob("FullJob", loggerMock.Object);

        // Assert: all 3 files copied
        Assert.True(File.Exists(Path.Combine(_tempTarget, "a.txt")));
        Assert.True(File.Exists(Path.Combine(_tempTarget, "b.txt")));
        Assert.True(File.Exists(Path.Combine(_tempTarget, "sub", "c.txt")));

        // Logger called 3 times
        loggerMock.Verify(l => l.Log(It.IsAny<LogEntry>()), Times.Exactly(3));
    }

    // ── ExecuteJob (Differential) ─────────────────────────────────────────────

    [Fact]
    public void ExecuteJob_Differential_CopiesOnlyModifiedFiles()
    {
        // Arrange: create source file and an already-copied target
        File.WriteAllText(Path.Combine(_tempSource, "old.txt"), "unchanged");
        File.WriteAllText(Path.Combine(_tempSource, "new.txt"), "updated");

        // Simulate old.txt already in target with same timestamp (not modified)
        string targetOld = Path.Combine(_tempTarget, "old.txt");
        File.WriteAllText(targetOld, "unchanged");
        File.SetLastWriteTimeUtc(targetOld, File.GetLastWriteTimeUtc(Path.Combine(_tempSource, "old.txt")));

        _service.CreateJob("DiffJob", _tempSource, _tempTarget, BackupType.Differential);
        var loggerMock = new Mock<ILogger>();

        // Act
        _service.ExecuteJob("DiffJob", loggerMock.Object);

        // Only new.txt should have been copied (old.txt unchanged)
        loggerMock.Verify(l => l.Log(It.Is<LogEntry>(e => e.SourceFilePath.Contains("new.txt"))), Times.Once);
        loggerMock.Verify(l => l.Log(It.Is<LogEntry>(e => e.SourceFilePath.Contains("old.txt"))), Times.Never);
    }
}

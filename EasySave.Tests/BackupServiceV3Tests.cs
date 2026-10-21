using System.Diagnostics;
using EasySave.Core.Models;
using EasySave.Core.Services;
using EasyLog;
using Moq;

namespace EasySave.Tests;

/// <summary>Shared temporary files and folders for the v3.0 tests.</summary>
public abstract class TempWorkspace : IDisposable
{
    private readonly List<string> _paths = [];

    protected string NewDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"es_v3_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        _paths.Add(dir);
        return dir;
    }

    protected string NewFile(string extension = ".json")
    {
        string file = Path.Combine(Path.GetTempPath(), $"es_v3_{Guid.NewGuid():N}{extension}");
        _paths.Add(file);
        return file;
    }

    protected static void WriteFiles(string dir, params string[] names)
    {
        foreach (var name in names)
            File.WriteAllText(Path.Combine(dir, name), $"content of {name}");
    }

    public void Dispose()
    {
        foreach (var path in _paths)
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
            else if (File.Exists(path)) File.Delete(path);
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>Unit tests for the priority rule and the Play/Pause/Stop controller.</summary>
public sealed class PriorityAndControllerTests
{
    [Fact]
    public async Task PriorityGuard_BlocksNonPriorityUntilAllPriorityFilesAreReleased()
    {
        var guard = new PriorityTransferGuard();
        guard.Register(2);

        var wait = guard.WaitUntilNoPriorityPendingAsync(CancellationToken.None);
        await Task.Delay(150);
        Assert.False(wait.IsCompleted);

        guard.Release();
        await Task.Delay(150);
        Assert.False(wait.IsCompleted);

        guard.Release();
        await wait.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(0, guard.PendingCount);
    }

    [Fact]
    public async Task PriorityGuard_WaitIsCancelledWhenJobIsStopped()
    {
        var guard = new PriorityTransferGuard();
        guard.Register(1);
        using var cts = new CancellationTokenSource(100);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => guard.WaitUntilNoPriorityPendingAsync(cts.Token));
    }

    [Fact]
    public void JobController_UserResumeDoesNotOverrideBusinessSoftwarePause()
    {
        using var controller = new JobController();
        controller.SetBusinessSoftwarePause(true);
        controller.Pause();

        controller.Resume();
        Assert.True(controller.IsPaused); // Still paused: the business software is running

        controller.SetBusinessSoftwarePause(false);
        Assert.False(controller.IsPaused);
    }

    [Fact]
    public void JobController_BusinessSoftwareClosingDoesNotResumeUserPause()
    {
        using var controller = new JobController();
        controller.Pause();
        controller.SetBusinessSoftwarePause(true);
        controller.SetBusinessSoftwarePause(false);

        Assert.True(controller.IsPaused);
    }
}

/// <summary>Unit tests for the shared <see cref="BackupEngine"/>.</summary>
public sealed class BackupEngineTests : TempWorkspace
{
    private readonly StateManager _stateManager;
    private readonly PriorityTransferGuard _guard = new();
    private readonly BackupEngine _engine;

    public BackupEngineTests()
    {
        _stateManager = new StateManager(NewFile());
        _engine = new BackupEngine(_stateManager, _guard, new LargeFileTransferGuard());
    }

    private BackupJob NewJob(params string[] files)
    {
        string src = NewDir();
        WriteFiles(src, files);
        return new BackupJob { Name = $"Job{Guid.NewGuid():N}"[..10], SourceDirectory = src, TargetDirectory = NewDir() };
    }

    private static IReadOnlySet<string> Ext(params string[] extensions) => BackupRunOptions.ToExtensionSet(extensions);

    [Fact]
    public void Prepare_PutsPriorityFilesFirstAndRegistersThem()
    {
        var job = NewJob("a.txt", "b.docx", "c.txt", "d.DOCX");

        var plan = _engine.Prepare(job, Ext(".docx"));

        Assert.Equal(2, plan.PriorityFileCount);
        Assert.True(plan.Files[0].IsPriority && plan.Files[1].IsPriority);
        Assert.False(plan.Files[2].IsPriority || plan.Files[3].IsPriority);
        Assert.Equal(2, _guard.PendingCount);
    }

    [Fact]
    public async Task Run_Completed_ReleasesEveryPriorityFile()
    {
        var job = NewJob("a.docx", "b.txt");
        var options = new BackupRunOptions { PriorityExtensions = Ext(".docx") };

        var outcome = await _engine.RunAsync(_engine.Prepare(job, options.PriorityExtensions), Mock.Of<ILogger>(), options);

        Assert.Equal(BackupOutcome.Completed, outcome);
        Assert.Equal(0, _guard.PendingCount);
        Assert.True(File.Exists(Path.Combine(job.TargetDirectory, "b.txt")));
    }

    [Fact]
    public async Task Run_Stopped_ReleasesRemainingPriorityFiles_SoOtherJobsAreNotBlocked()
    {
        var job = NewJob("a.docx", "b.docx", "c.txt");
        using var controller = new JobController();
        controller.Stop();
        var options = new BackupRunOptions { PriorityExtensions = Ext(".docx"), Controller = controller };

        var outcome = await _engine.RunAsync(_engine.Prepare(job, options.PriorityExtensions), Mock.Of<ILogger>(), options);

        Assert.Equal(BackupOutcome.Stopped, outcome);
        Assert.Equal(0, _guard.PendingCount);
        Assert.Equal(JobStatus.Stopped, _stateManager.GetAllStates().Single().Status);
    }

    [Fact]
    public async Task Run_NonPriorityFileWaitsForPriorityFilesOfOtherJobs()
    {
        var otherJob = NewJob("urgent.docx");
        var otherPlan = _engine.Prepare(otherJob, Ext(".docx")); // Registered, not transferred yet

        var job = NewJob("normal.txt");
        var run = _engine.RunAsync(_engine.Prepare(job, Ext(".docx")), Mock.Of<ILogger>(), BackupRunOptions.Default);
        await Task.Delay(300);
        Assert.False(run.IsCompleted);
        Assert.False(File.Exists(Path.Combine(job.TargetDirectory, "normal.txt")));

        await _engine.RunAsync(otherPlan, Mock.Of<ILogger>(), new BackupRunOptions { PriorityExtensions = Ext(".docx") });
        Assert.Equal(BackupOutcome.Completed, await run.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task Run_StateIsNeverPausedWhenTheJobIsNotPaused()
    {
        var job = NewJob("a.txt", "b.txt", "c.txt");
        using var controller = new JobController();
        var statuses = new List<JobStatus>();
        var logger = new Mock<ILogger>();
        logger.Setup(l => l.Log(It.IsAny<LogEntry>()))
              .Callback(() => statuses.Add(_stateManager.GetAllStates().Single().Status));

        await _engine.RunAsync(_engine.Prepare(job, Ext()), logger.Object, new BackupRunOptions { Controller = controller });

        Assert.Equal(3, statuses.Count);
        Assert.All(statuses, s => Assert.Equal(JobStatus.Active, s));
    }

    [Fact]
    public async Task Run_InterruptRequested_FinishesCurrentFileThenStopsAndLogsIt()
    {
        var job = NewJob("a.txt", "b.txt", "c.txt");
        int checks = 0;
        var entries = new List<LogEntry>();
        var logger = new Mock<ILogger>();
        logger.Setup(l => l.Log(It.IsAny<LogEntry>())).Callback<LogEntry>(entries.Add);
        var options = new BackupRunOptions { InterruptRequested = () => ++checks > 1 };

        var outcome = await _engine.RunAsync(_engine.Prepare(job, Ext()), logger.Object, options);

        Assert.Equal(BackupOutcome.Interrupted, outcome);
        Assert.Single(Directory.GetFiles(job.TargetDirectory));
        Assert.Equal(LogActions.BusinessSoftwareInterrupted, entries[^1].Action);
    }

    [Fact]
    public async Task Run_LogsDirectoryCreation()
    {
        var job = NewJob();
        Directory.CreateDirectory(Path.Combine(job.SourceDirectory, "sub"));
        WriteFiles(Path.Combine(job.SourceDirectory, "sub"), "x.txt");
        var entries = new List<LogEntry>();
        var logger = new Mock<ILogger>();
        logger.Setup(l => l.Log(It.IsAny<LogEntry>())).Callback<LogEntry>(entries.Add);

        await _engine.RunAsync(_engine.Prepare(job, Ext()), logger.Object, BackupRunOptions.Default);

        Assert.Contains(entries, e => e.Action == LogActions.DirectoryCreated);
        Assert.Contains(entries, e => e.Action == LogActions.FileTransfer);
    }
}

/// <summary>
/// Tests of <see cref="BackupServiceV3"/> (kept in one class: they share the application-wide guards).
/// </summary>
public sealed class BackupServiceV3Tests : TempWorkspace
{
    private static readonly string CurrentProcess = Process.GetCurrentProcess().ProcessName;

    private readonly StateManager _stateManager;
    private readonly BusinessSoftwareMonitor _monitor = new();
    private readonly BackupServiceV3 _service;
    private readonly List<LogEntry> _entries = [];
    private readonly ILogger _logger;

    public BackupServiceV3Tests()
    {
        _stateManager = new StateManager(NewFile());
        _service = new BackupServiceV3(_stateManager, new AppSettings(NewFile()), _monitor, NewFile());
        var logger = new Mock<ILogger>();
        logger.Setup(l => l.Log(It.IsAny<LogEntry>())).Callback<LogEntry>(e => { lock (_entries) _entries.Add(e); });
        _logger = logger.Object;
    }

    private string CreateJob(string name, params string[] files)
    {
        string src = NewDir();
        WriteFiles(src, files);
        Assert.Equal(BackupCreationResult.Success, _service.CreateJob(name, src, NewDir(), BackupType.Full));
        return _service.GetJob(name)!.TargetDirectory;
    }

    private JobStatus StatusOf(string job) => _stateManager.GetAllStates().Single(s => s.Name == job).Status;

    [Fact]
    public async Task BusinessSoftwareAlreadyRunning_JobStartsPausedThenResumesAutomatically()
    {
        string target = CreateJob("PausedAtStart", "a.txt");
        _monitor.ProcessName = CurrentProcess; // The test host plays the business software

        var run = _service.ExecuteJobsParallelAsync(["PausedAtStart"], _logger);
        await Task.Delay(700);
        Assert.False(File.Exists(Path.Combine(target, "a.txt")));
        Assert.Equal(JobStatus.Paused, StatusOf("PausedAtStart"));

        _monitor.ProcessName = string.Empty; // Business software closed
        await run.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(File.Exists(Path.Combine(target, "a.txt")));
        Assert.Equal(JobStatus.Completed, StatusOf("PausedAtStart"));
        lock (_entries)
        {
            Assert.Contains(_entries, e => e.Action == LogActions.BusinessSoftwarePaused);
            Assert.Contains(_entries, e => e.Action == LogActions.BusinessSoftwareResumed);
        }
    }

    [Fact]
    public async Task StopJob_WhilePaused_EndsTheJobAsStopped()
    {
        string target = CreateJob("ToStop", "a.txt", "b.txt");
        _monitor.ProcessName = CurrentProcess;

        var run = _service.ExecuteJobsParallelAsync(["ToStop"], _logger);
        await Task.Delay(300);
        Assert.True(_service.IsJobRunning("ToStop"));
        _service.StopJob("ToStop");
        await run.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(_service.IsJobRunning("ToStop"));
        Assert.Empty(Directory.GetFiles(target));
        Assert.Equal(JobStatus.Stopped, StatusOf("ToStop"));
        Assert.Equal(0, PriorityTransferGuard.Instance.PendingCount);
    }

    [Fact]
    public async Task ParallelJobs_AllComplete()
    {
        string t1 = CreateJob("Parallel1", "a.txt", "b.txt");
        string t2 = CreateJob("Parallel2", "c.txt");

        await _service.ExecuteJobsParallelAsync(["Parallel1", "Parallel2"], _logger).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(2, Directory.GetFiles(t1).Length);
        Assert.Single(Directory.GetFiles(t2));
        Assert.False(_service.HasRunningJobs);
    }

    [Fact]
    public void StateFile_WritesStatusAsText()
    {
        string path = NewFile();
        new StateManager(path).UpdateJobState(new JobState { Name = "Readable", Status = JobStatus.Completed });

        Assert.Contains("\"Status\": \"Completed\"", File.ReadAllText(path));
    }
}

/// <summary>Tests of the v3.0 log centralization settings and entries.</summary>
public sealed class LogCentralizationTests : TempWorkspace
{
    [Fact]
    public void LogEntry_IncludesMachineNameAndUserName()
    {
        var entry = new LogEntry { BackupName = "TestJob" };

        Assert.Equal(Environment.MachineName, entry.MachineName);
        Assert.Equal(Environment.UserName, entry.UserName);
    }

    [Fact]
    public void AppSettings_LogModeAndServer_PersistAndReload()
    {
        string path = NewFile();
        var settings = new AppSettings(path)
        {
            LogMode = LogMode.Centralized,
            LogServerAddress = "192.168.1.100",
            LogServerPort = 9090
        };

        var reloaded = new AppSettings(path);
        Assert.Equal(LogMode.Centralized, reloaded.LogMode);
        Assert.Equal("192.168.1.100", reloaded.LogServerAddress);
        Assert.Equal(9090, reloaded.LogServerPort);
        Assert.Equal(LogMode.Local, new AppSettings(NewFile()).LogMode); // Default
    }

    [Theory]
    [InlineData(LogMode.Local, typeof(JsonLogger))]
    [InlineData(LogMode.Centralized, typeof(SocketLogger))]
    [InlineData(LogMode.Both, typeof(CompositeLogger))]
    public void LoggerFactory_CreatesTheLoggerOfEachMode(LogMode mode, Type expected)
    {
        var logger = LoggerFactory.Create(mode, LogFormat.Json, NewDir(), "127.0.0.1", 8080);

        Assert.IsType(expected, logger);
        (logger as IDisposable)?.Dispose();
    }

    [Fact]
    public void JsonLogger_ManyEntriesWithAccents_StaysOneValidArray()
    {
        string dir = NewDir();
        var logger = new JsonLogger(dir);
        for (int i = 0; i < 60; i++)
            logger.Log(new LogEntry { BackupName = $"Génie {i}", SourceFilePath = $@"C:\Données\fichier_{i}.txt" });

        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Directory.GetFiles(dir).Single()));
        Assert.Equal(60, doc.RootElement.GetArrayLength());
        Assert.Equal("Génie 59", doc.RootElement[59].GetProperty("backupName").GetString());
    }

    [Fact]
    public void XmlLogger_WritesMachineUserAndAction()
    {
        string dir = NewDir();
        new XmlLogger(dir).Log(new LogEntry { BackupName = "X", Action = LogActions.JobStopped });

        var doc = new System.Xml.XmlDocument();
        doc.Load(Directory.GetFiles(dir).Single());
        var entry = doc.DocumentElement!.FirstChild!;
        Assert.Equal(Environment.MachineName, entry["MachineName"]!.InnerText);
        Assert.Equal(Environment.UserName, entry["UserName"]!.InnerText);
        Assert.Equal(LogActions.JobStopped, entry["Action"]!.InnerText);
    }
}

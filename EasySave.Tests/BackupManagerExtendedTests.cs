using System;
using System.IO;
using EasySave.Core;
using EasyLog;
using Moq;
using Xunit;

namespace EasySave.Tests
{
    public class BackupManagerExtendedTests : IDisposable
    {
        private readonly string tempDir;

        public BackupManagerExtendedTests()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "EasySaveMgrTests_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);
        }

        private (BackupManager manager, string src, string dst) CreateManagerWithDirs(string suffix = "")
        {
            var src = Path.Combine(tempDir, "src" + suffix);
            var dst = Path.Combine(tempDir, "dst" + suffix);
            Directory.CreateDirectory(src);
            Directory.CreateDirectory(dst);
            var stateManager = new StateManager(Path.Combine(tempDir, Guid.NewGuid() + "_state.json"));
            var loggerMock = new Mock<ILogger>();
            var manager = new BackupManager(stateManager, loggerMock.Object, Path.Combine(tempDir, Guid.NewGuid() + "_config.json"));
            return (manager, src, dst);
        }

        [Fact]
        public void CreateBackupJob_DuplicateName_ReturnsDuplicateNameError()
        {
            var (manager, src, dst) = CreateManagerWithDirs("dup");
            manager.CreateBackupJob("DupJob", src, dst, BackupType.Full, out _);

            bool ok = manager.CreateBackupJob("DupJob", src, dst, BackupType.Full, out var error);

            Assert.False(ok);
            Assert.Equal(BackupManager.BackupCreationError.DuplicateName, error);
        }

        [Fact]
        public void CreateBackupJob_DuplicateNameCaseInsensitive_ReturnsDuplicateNameError()
        {
            var (manager, src, dst) = CreateManagerWithDirs("dupci");
            manager.CreateBackupJob("MyJob", src, dst, BackupType.Full, out _);

            bool ok = manager.CreateBackupJob("myjob", src, dst, BackupType.Full, out var error);

            Assert.False(ok);
            Assert.Equal(BackupManager.BackupCreationError.DuplicateName, error);
        }

        [Fact]
        public void CreateBackupJob_MaxJobsExceeded_ReturnsMaxJobsExceededError()
        {
            var (manager, src, dst) = CreateManagerWithDirs("max");
            for (int i = 1; i <= 5; i++)
            {
                var si = Path.Combine(tempDir, $"src_max_{i}");
                var di = Path.Combine(tempDir, $"dst_max_{i}");
                Directory.CreateDirectory(si);
                Directory.CreateDirectory(di);
                manager.CreateBackupJob($"Job{i}", si, di, BackupType.Full, out _);
            }

            var extraSrc = Path.Combine(tempDir, "src_extra");
            var extraDst = Path.Combine(tempDir, "dst_extra");
            Directory.CreateDirectory(extraSrc);
            Directory.CreateDirectory(extraDst);
            bool ok = manager.CreateBackupJob("Job6", extraSrc, extraDst, BackupType.Full, out var error);

            Assert.False(ok);
            Assert.Equal(BackupManager.BackupCreationError.MaxJobsExceeded, error);
        }

        [Fact]
        public void CreateBackupJob_SourcePathInvalid_ReturnsSourcePathInvalid()
        {
            var (manager, _, dst) = CreateManagerWithDirs("si");

            bool ok = manager.CreateBackupJob("JobSI", "", dst, BackupType.Full, out var error);

            Assert.False(ok);
            Assert.Equal(BackupManager.BackupCreationError.SourcePathInvalid, error);
        }

        [Fact]
        public void CreateBackupJob_SourceNotFound_ReturnsSourcePathNotFound()
        {
            var (manager, _, dst) = CreateManagerWithDirs("snf");

            bool ok = manager.CreateBackupJob("JobSNF", Path.Combine(tempDir, "nonexistent_src"), dst, BackupType.Full, out var error);

            Assert.False(ok);
            Assert.Equal(BackupManager.BackupCreationError.SourcePathNotFound, error);
        }

        [Fact]
        public void CreateBackupJob_TargetPathInvalid_ReturnsTargetPathInvalid()
        {
            var (manager, src, _) = CreateManagerWithDirs("ti");

            bool ok = manager.CreateBackupJob("JobTI", src, "", BackupType.Full, out var error);

            Assert.False(ok);
            Assert.Equal(BackupManager.BackupCreationError.TargetPathInvalid, error);
        }

        [Fact]
        public void CreateBackupJob_TargetNotFound_ReturnsTargetPathNotFound()
        {
            var (manager, src, _) = CreateManagerWithDirs("tnf");

            bool ok = manager.CreateBackupJob("JobTNF", src, Path.Combine(tempDir, "nonexistent_dst"), BackupType.Full, out var error);

            Assert.False(ok);
            Assert.Equal(BackupManager.BackupCreationError.TargetPathNotFound, error);
        }

        [Fact]
        public void CreateBackupJob_NameTooLong_ReturnsInvalidNameError()
        {
            var (manager, src, dst) = CreateManagerWithDirs("longname");
            string longName = new string('a', 51);

            bool ok = manager.CreateBackupJob(longName, src, dst, BackupType.Full, out var error);

            Assert.False(ok);
            Assert.Equal(BackupManager.BackupCreationError.InvalidName, error);
        }

        [Fact]
        public void CreateBackupJob_NameWithSpecialChars_ReturnsInvalidNameError()
        {
            var (manager, src, dst) = CreateManagerWithDirs("special");

            bool ok = manager.CreateBackupJob("job@#$!", src, dst, BackupType.Full, out var error);

            Assert.False(ok);
            Assert.Equal(BackupManager.BackupCreationError.InvalidName, error);
        }

        [Fact]
        public void DeleteBackupJob_ValidIndex_RemovesJob()
        {
            var (manager, src, dst) = CreateManagerWithDirs("del");
            manager.CreateBackupJob("ToDelete", src, dst, BackupType.Full, out _);

            bool ok = manager.DeleteBackupJob(0);

            Assert.True(ok);
            Assert.Empty(manager.GetBackupJobs());
        }

        [Fact]
        public void DeleteBackupJob_NegativeIndex_ReturnsFalse()
        {
            var (manager, _, _) = CreateManagerWithDirs("delneg");

            bool ok = manager.DeleteBackupJob(-1);

            Assert.False(ok);
        }

        [Fact]
        public void DeleteBackupJob_IndexOutOfRange_ReturnsFalse()
        {
            var (manager, src, dst) = CreateManagerWithDirs("deloor");
            manager.CreateBackupJob("Job", src, dst, BackupType.Full, out _);

            bool ok = manager.DeleteBackupJob(5);

            Assert.False(ok);
        }

        [Fact]
        public void GetBackupJobs_ReturnsAllCreatedJobs()
        {
            var (manager, src, dst) = CreateManagerWithDirs("getall");
            manager.CreateBackupJob("JobA", src, dst, BackupType.Full, out _);
            manager.CreateBackupJob("JobB", src, dst, BackupType.Differential, out _);

            var jobs = manager.GetBackupJobs();

            Assert.Equal(2, jobs.Count);
        }

        [Fact]
        public void GetBackupJobs_Empty_ReturnsEmptyList()
        {
            var (manager, _, _) = CreateManagerWithDirs("empty");
            Assert.Empty(manager.GetBackupJobs());
        }

        [Fact]
        public void AddMonitoredProcess_AddsToMonitor()
        {
            var (manager, _, _) = CreateManagerWithDirs("addproc");
            manager.AddMonitoredProcess("notepad");
            Assert.Contains("notepad", manager.GetMonitoredProcesses());
        }

        [Fact]
        public void RemoveMonitoredProcess_RemovesFromMonitor()
        {
            var (manager, _, _) = CreateManagerWithDirs("remproc");
            manager.AddMonitoredProcess("notepad");
            manager.RemoveMonitoredProcess("notepad");
            Assert.DoesNotContain("notepad", manager.GetMonitoredProcesses());
        }

        [Fact]
        public void ClearMonitoredProcesses_RemovesAll()
        {
            var (manager, _, _) = CreateManagerWithDirs("clearproc");
            manager.AddMonitoredProcess("notepad");
            manager.AddMonitoredProcess("calc");
            manager.ClearMonitoredProcesses();
            Assert.Empty(manager.GetMonitoredProcesses());
        }

        [Fact]
        public void IsBusinessSoftwareRunning_NoProcesses_ReturnsFalse()
        {
            var (manager, _, _) = CreateManagerWithDirs("nobiz");
            Assert.False(manager.IsBusinessSoftwareRunning());
        }

        [Fact]
        public void AddPriorityExtension_AddsNormalizedExtension()
        {
            var (manager, _, _) = CreateManagerWithDirs("priext");
            manager.AddPriorityExtension(".pdf");
            Assert.Contains(".pdf", manager.GetPriorityExtensions());
        }

        [Fact]
        public void AddPriorityExtension_WithoutDot_AddsDotPrefix()
        {
            var (manager, _, _) = CreateManagerWithDirs("priextdot");
            manager.AddPriorityExtension("pdf");
            Assert.Contains(".pdf", manager.GetPriorityExtensions());
        }

        [Fact]
        public void AddPriorityExtension_Duplicate_NotAddedTwice()
        {
            var (manager, _, _) = CreateManagerWithDirs("priextdup");
            manager.AddPriorityExtension(".pdf");
            manager.AddPriorityExtension(".pdf");
            Assert.Single(manager.GetPriorityExtensions());
        }

        [Fact]
        public void RemovePriorityExtension_RemovesExtension()
        {
            var (manager, _, _) = CreateManagerWithDirs("remext");
            manager.AddPriorityExtension(".pdf");
            manager.RemovePriorityExtension(".pdf");
            Assert.Empty(manager.GetPriorityExtensions());
        }

        [Fact]
        public void ClearPriorityExtensions_RemovesAll()
        {
            var (manager, _, _) = CreateManagerWithDirs("clearext");
            manager.AddPriorityExtension(".pdf");
            manager.AddPriorityExtension(".docx");
            manager.ClearPriorityExtensions();
            Assert.Empty(manager.GetPriorityExtensions());
        }

        [Fact]
        public void SaveConfiguration_CreatesConfigFile()
        {
            var configPath = Path.Combine(tempDir, "save_config_test.json");
            var src = Path.Combine(tempDir, "src_save");
            var dst = Path.Combine(tempDir, "dst_save");
            Directory.CreateDirectory(src);
            Directory.CreateDirectory(dst);
            var stateManager = new StateManager(Path.Combine(tempDir, "state_save.json"));
            var loggerMock = new Mock<ILogger>();
            var manager = new BackupManager(stateManager, loggerMock.Object, configPath);

            manager.CreateBackupJob("SaveJob", src, dst, BackupType.Full, out _);

            Assert.True(File.Exists(configPath));
        }

        [Fact]
        public void LoadConfiguration_RestoresJobs()
        {
            var configPath = Path.Combine(tempDir, "load_config_test.json");
            var src = Path.Combine(tempDir, "src_load");
            var dst = Path.Combine(tempDir, "dst_load");
            Directory.CreateDirectory(src);
            Directory.CreateDirectory(dst);
            var stateManager = new StateManager(Path.Combine(tempDir, "state_load.json"));
            var loggerMock = new Mock<ILogger>();

            var manager1 = new BackupManager(stateManager, loggerMock.Object, configPath);
            manager1.CreateBackupJob("LoadedJob", src, dst, BackupType.Differential, out _);

            // Create a new manager that will load the config
            var manager2 = new BackupManager(stateManager, loggerMock.Object, configPath);

            var jobs = manager2.GetBackupJobs();
            Assert.Single(jobs);
            Assert.Equal("LoadedJob", jobs[0].GetName());
            Assert.Equal(BackupType.Differential, jobs[0].GetBackupType());
        }

        [Fact]
        public void CreateBackupJob_DifferentialType_SetTypeCorrectly()
        {
            var (manager, src, dst) = CreateManagerWithDirs("diff");
            manager.CreateBackupJob("DiffJob", src, dst, BackupType.Differential, out _);
            var jobs = manager.GetBackupJobs();
            Assert.Equal(BackupType.Differential, jobs[0].GetBackupType());
        }

        public void Dispose()
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }
}

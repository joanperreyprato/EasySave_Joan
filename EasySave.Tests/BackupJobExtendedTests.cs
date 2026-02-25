using System;
using System.IO;
using EasySave.Core;
using EasyLog;
using Moq;
using Xunit;

namespace EasySave.Tests
{
    public class BackupJobExtendedTests : IDisposable
    {
        private readonly string tempDir;

        public BackupJobExtendedTests()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "EasySaveJobExtTests_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);
        }

        private (BackupJob job, StateManager stateManager, Mock<ILogger> loggerMock)
            CreateJob(string name, string src, string dst, BackupType type = BackupType.Full)
        {
            var stateManager = new StateManager(Path.Combine(tempDir, Guid.NewGuid() + "_state.json"));
            var loggerMock = new Mock<ILogger>();
            var job = new BackupJob(name, src, dst, type);
            return (job, stateManager, loggerMock);
        }

        [Fact]
        public void GetName_ReturnsCorrectName()
        {
            var job = new BackupJob("TestJob", "/src", "/dst", BackupType.Full);
            Assert.Equal("TestJob", job.GetName());
        }

        [Fact]
        public void GetSourcePath_ReturnsCorrectPath()
        {
            var job = new BackupJob("J", "/my/source", "/dst", BackupType.Full);
            Assert.Equal("/my/source", job.GetSourcePath());
        }

        [Fact]
        public void GetTargetPath_ReturnsCorrectPath()
        {
            var job = new BackupJob("J", "/src", "/my/target", BackupType.Full);
            Assert.Equal("/my/target", job.GetTargetPath());
        }

        [Fact]
        public void GetBackupType_Full_ReturnsFullType()
        {
            var job = new BackupJob("J", "/src", "/dst", BackupType.Full);
            Assert.Equal(BackupType.Full, job.GetBackupType());
        }

        [Fact]
        public void GetBackupType_Differential_ReturnsDifferentialType()
        {
            var job = new BackupJob("J", "/src", "/dst", BackupType.Differential);
            Assert.Equal(BackupType.Differential, job.GetBackupType());
        }

        [Fact]
        public void GetState_InitialState_IsInactive()
        {
            var job = new BackupJob("J", "/src", "/dst", BackupType.Full);
            Assert.Equal(StateStatus.Inactive, job.GetState().Status);
        }

        [Fact]
        public void GetLogFormat_Default_IsJson()
        {
            var job = new BackupJob("J", "/src", "/dst", BackupType.Full);
            Assert.Equal(EasyLog.LogFormat.Json, job.GetLogFormat());
        }

        [Fact]
        public void GetLogFormat_Xml_IsXml()
        {
            var job = new BackupJob("J", "/src", "/dst", BackupType.Full, EasyLog.LogFormat.Xml);
            Assert.Equal(EasyLog.LogFormat.Xml, job.GetLogFormat());
        }

        [Fact]
        public void Execute_MissingSource_SetsErrorState()
        {
            var src = Path.Combine(tempDir, "missing_src_execute");
            var dst = Path.Combine(tempDir, "dst_missing");
            Directory.CreateDirectory(dst);
            var (job, stateManager, loggerMock) = CreateJob("ErrJob", src, dst);

            job.Execute(loggerMock.Object, stateManager);

            Assert.Equal(StateStatus.Error, job.GetState().Status);
        }

        [Fact]
        public void Execute_EmptySourceDirectory_SetsCompletedState()
        {
            var src = Path.Combine(tempDir, "empty_src");
            var dst = Path.Combine(tempDir, "empty_dst");
            Directory.CreateDirectory(src);
            Directory.CreateDirectory(dst);
            var (job, stateManager, loggerMock) = CreateJob("EmptyJob", src, dst);

            job.Execute(loggerMock.Object, stateManager);

            Assert.Equal(StateStatus.Completed, job.GetState().Status);
        }

        [Fact]
        public void Execute_EmptySourceDirectory_Progress100()
        {
            var src = Path.Combine(tempDir, "empty_src2");
            var dst = Path.Combine(tempDir, "empty_dst2");
            Directory.CreateDirectory(src);
            Directory.CreateDirectory(dst);
            var (job, stateManager, loggerMock) = CreateJob("EmptyJob2", src, dst);

            job.Execute(loggerMock.Object, stateManager);

            Assert.Equal(100.0, job.GetState().Progress);
        }

        [Fact]
        public void Execute_SingleFile_CopiesFileToDestination()
        {
            var src = Path.Combine(tempDir, "single_src");
            var dst = Path.Combine(tempDir, "single_dst");
            Directory.CreateDirectory(src);
            Directory.CreateDirectory(dst);
            File.WriteAllText(Path.Combine(src, "hello.txt"), "world");
            var (job, stateManager, loggerMock) = CreateJob("SingleJob", src, dst);

            job.Execute(loggerMock.Object, stateManager);

            Assert.True(File.Exists(Path.Combine(dst, "hello.txt")));
            Assert.Equal("world", File.ReadAllText(Path.Combine(dst, "hello.txt")));
        }

        [Fact]
        public void Execute_MultipleFiles_AllCopied()
        {
            var src = Path.Combine(tempDir, "multi_src");
            var dst = Path.Combine(tempDir, "multi_dst");
            Directory.CreateDirectory(src);
            Directory.CreateDirectory(dst);
            for (int i = 1; i <= 5; i++)
                File.WriteAllText(Path.Combine(src, $"file{i}.txt"), $"content{i}");
            var (job, stateManager, loggerMock) = CreateJob("MultiJob", src, dst);

            job.Execute(loggerMock.Object, stateManager);

            for (int i = 1; i <= 5; i++)
                Assert.True(File.Exists(Path.Combine(dst, $"file{i}.txt")));
            Assert.Equal(StateStatus.Completed, job.GetState().Status);
        }

        [Fact]
        public void Execute_NestedDirectories_PreservesStructure()
        {
            var src = Path.Combine(tempDir, "nested_src");
            var dst = Path.Combine(tempDir, "nested_dst");
            var subDir = Path.Combine(src, "subdir");
            Directory.CreateDirectory(src);
            Directory.CreateDirectory(dst);
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(subDir, "nested.txt"), "nested content");
            var (job, stateManager, loggerMock) = CreateJob("NestedJob", src, dst);

            job.Execute(loggerMock.Object, stateManager);

            Assert.True(File.Exists(Path.Combine(dst, "subdir", "nested.txt")));
            Assert.Equal("nested content", File.ReadAllText(Path.Combine(dst, "subdir", "nested.txt")));
        }

        [Fact]
        public void Execute_UpdatesStateManagerWithJobState()
        {
            var src = Path.Combine(tempDir, "state_src");
            var dst = Path.Combine(tempDir, "state_dst");
            Directory.CreateDirectory(src);
            Directory.CreateDirectory(dst);
            File.WriteAllText(Path.Combine(src, "file.txt"), "data");
            var (job, stateManager, loggerMock) = CreateJob("StateJob", src, dst);

            job.Execute(loggerMock.Object, stateManager);

            var state = stateManager.GetJobState("StateJob");
            Assert.NotNull(state);
            Assert.Equal(StateStatus.Completed, state.Status);
        }

        [Fact]
        public void Execute_LogsAtLeastOnce()
        {
            var src = Path.Combine(tempDir, "log_src");
            var dst = Path.Combine(tempDir, "log_dst");
            Directory.CreateDirectory(src);
            Directory.CreateDirectory(dst);
            File.WriteAllText(Path.Combine(src, "file.txt"), "data");
            var (job, stateManager, loggerMock) = CreateJob("LogJob", src, dst);

            job.Execute(loggerMock.Object, stateManager);

            loggerMock.Verify(l => l.LogAction(It.IsAny<LogEntry>()), Times.AtLeastOnce());
        }

        [Fact]
        public void Execute_NullStateManager_ThrowsArgumentNullException()
        {
            var src = Path.Combine(tempDir, "null_sm_src");
            var dst = Path.Combine(tempDir, "null_sm_dst");
            Directory.CreateDirectory(src);
            Directory.CreateDirectory(dst);
            var loggerMock = new Mock<ILogger>();
            var job = new BackupJob("NullSM", src, dst, BackupType.Full);

            Assert.Throws<ArgumentNullException>(() => job.Execute(loggerMock.Object, null!));
        }

        [Fact]
        public void Execute_OverwritesExistingFile_InDestination()
        {
            var src = Path.Combine(tempDir, "overwrite_src");
            var dst = Path.Combine(tempDir, "overwrite_dst");
            Directory.CreateDirectory(src);
            Directory.CreateDirectory(dst);
            File.WriteAllText(Path.Combine(src, "file.txt"), "new content");
            File.WriteAllText(Path.Combine(dst, "file.txt"), "old content");
            var (job, stateManager, loggerMock) = CreateJob("OverwriteJob", src, dst);

            job.Execute(loggerMock.Object, stateManager);

            Assert.Equal("new content", File.ReadAllText(Path.Combine(dst, "file.txt")));
        }

        [Fact]
        public void Execute_SetsProgress100_OnCompletion()
        {
            var src = Path.Combine(tempDir, "prog_src");
            var dst = Path.Combine(tempDir, "prog_dst");
            Directory.CreateDirectory(src);
            Directory.CreateDirectory(dst);
            File.WriteAllText(Path.Combine(src, "f.txt"), "x");
            var (job, stateManager, loggerMock) = CreateJob("ProgJob", src, dst);

            job.Execute(loggerMock.Object, stateManager);

            Assert.Equal(100.0, job.GetState().Progress);
        }

        public void Dispose()
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }
}

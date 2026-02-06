using System;
using System.IO;
using EasySave_v1._0;
using EasyLog;
using Moq;
using Xunit;

namespace EasySave.Tests
{
    public class BackupManagerTests : IDisposable
    {
        private readonly string tempDir;

        public BackupManagerTests()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "EasySaveTests_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);
        }

        [Fact]
        public void CreateBackupJob_ValidParameters_ReturnsTrueAndSavesConfig()
        {
            // Arrange
            var source = Path.Combine(tempDir, "src");
            var target = Path.Combine(tempDir, "dst");
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(target);

            var stateManager = new StateManager(Path.Combine(tempDir, "state.json"));
            var loggerMock = new Mock<ILogger>();
            var manager = new BackupManager(stateManager, loggerMock.Object, Path.Combine(tempDir, "config.json"));

            // Act
            bool ok = manager.CreateBackupJob("Job1", source, target, BackupType.Full, out var error);

            // Assert
            Assert.True(ok);
            Assert.Equal(BackupManager.BackupCreationError.None, error);
            var jobs = manager.GetBackupJobs();
            Assert.Single(jobs);
            Assert.Equal("Job1", jobs[0].GetName());
        }

        [Fact]
        public void CreateBackupJob_InvalidName_ReturnsInvalidNameError()
        {
            // Arrange
            var source = Path.Combine(tempDir, "src2");
            var target = Path.Combine(tempDir, "dst2");
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(target);

            var stateManager = new StateManager(Path.Combine(tempDir, "state.json"));
            var loggerMock = new Mock<ILogger>();
            var manager = new BackupManager(stateManager, loggerMock.Object, Path.Combine(tempDir, "config2.json"));

            // Act
            bool ok = manager.CreateBackupJob("", source, target, BackupType.Full, out var error);

            // Assert
            Assert.False(ok);
            Assert.Equal(BackupManager.BackupCreationError.InvalidName, error);
        }

        public void Dispose()
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }
}

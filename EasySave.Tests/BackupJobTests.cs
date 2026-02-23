using System;
using System.IO;
using EasySave.Core;
using EasyLog;
using Moq;
using Xunit;

namespace EasySave.Tests
{
    public class BackupJobTests : IDisposable
    {
        private readonly string tempDir;

        public BackupJobTests()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "EasySaveJobTests_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);
        }

        [Fact]
        public void Execute_FullBackup_CopiesFilesUpdatesStateAndLogs()
        {
            // Arrange
            var source = Path.Combine(tempDir, "src");
            var target = Path.Combine(tempDir, "dst");
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(target);

            // create a sample file
            var filePath = Path.Combine(source, "file.txt");
            File.WriteAllText(filePath, "hello");

            var stateFile = Path.Combine(tempDir, "state.json");
            var stateManager = new StateManager(stateFile);

            var loggerMock = new Mock<ILogger>();
            var job = new BackupJob("jobA", source, target, BackupType.Full);

            // Act
            job.Execute(loggerMock.Object, stateManager);

            // Assert - file copied
            var destFile = Path.Combine(target, "file.txt");
            Assert.True(File.Exists(destFile));
            Assert.Equal("hello", File.ReadAllText(destFile));

            // Assert - state updated to Completed
            var state = stateManager.GetJobState("jobA");
            Assert.NotNull(state);
            Assert.Equal(StateStatus.Completed, state.Status);

            // Assert - logger was called at least once (file and summary)
            loggerMock.Verify(l => l.LogAction(It.IsAny<LogEntry>()), Moq.Times.AtLeastOnce());
        }

        public void Dispose()
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using EasySave.Core;
using Xunit;

namespace EasySave.Tests
{
    public class StateManagerTests : IDisposable
    {
        private readonly string tempDir;

        public StateManagerTests()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "EasySaveStateTests_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);
        }

        private StateManager CreateManager() =>
            new StateManager(Path.Combine(tempDir, Guid.NewGuid() + "_state.json"));

        [Fact]
        public void UpdateJobState_NewJob_AddsToList()
        {
            var manager = CreateManager();
            var state = new JobState("job1") { Status = StateStatus.Active };

            manager.UpdateJobState("job1", state);

            var result = manager.GetJobState("job1");
            Assert.NotNull(result);
            Assert.Equal("job1", result.Name);
            Assert.Equal(StateStatus.Active, result.Status);
        }

        [Fact]
        public void UpdateJobState_ExistingJob_UpdatesProperties()
        {
            var manager = CreateManager();
            var state = new JobState("job1") { Status = StateStatus.Active, TotalFiles = 10 };
            manager.UpdateJobState("job1", state);

            var updated = new JobState("job1") { Status = StateStatus.Completed, TotalFiles = 10, RemainingFiles = 0 };
            manager.UpdateJobState("job1", updated);

            var result = manager.GetJobState("job1");
            Assert.Equal(StateStatus.Completed, result!.Status);
        }

        [Fact]
        public void GetJobState_NonExistentJob_ReturnsNull()
        {
            var manager = CreateManager();
            var result = manager.GetJobState("doesNotExist");
            Assert.Null(result);
        }

        [Fact]
        public void GetJobState_NullName_ReturnsNull()
        {
            var manager = CreateManager();
            var result = manager.GetJobState(null!);
            Assert.Null(result);
        }

        [Fact]
        public void GetJobState_EmptyName_ReturnsNull()
        {
            var manager = CreateManager();
            var result = manager.GetJobState("");
            Assert.Null(result);
        }

        [Fact]
        public void UpdateJobState_NullState_DoesNotThrow()
        {
            var manager = CreateManager();
            var ex = Record.Exception(() => manager.UpdateJobState("job1", null!));
            Assert.Null(ex);
        }

        [Fact]
        public void UpdateJobState_NullJobName_DoesNotThrow()
        {
            var manager = CreateManager();
            var state = new JobState("job1");
            var ex = Record.Exception(() => manager.UpdateJobState(null!, state));
            Assert.Null(ex);
        }

        [Fact]
        public void SaveState_CreatesFile()
        {
            var filePath = Path.Combine(tempDir, "save_test.json");
            var manager = new StateManager(filePath);
            manager.UpdateJobState("jobX", new JobState("jobX") { Status = StateStatus.Completed });

            manager.SaveState();

            Assert.True(File.Exists(filePath));
        }

        [Fact]
        public void LoadState_LoadsSavedStates()
        {
            var filePath = Path.Combine(tempDir, "load_test.json");
            var writer = new StateManager(filePath);
            writer.UpdateJobState("jobA", new JobState("jobA") { Status = StateStatus.Completed, TotalFiles = 5 });
            writer.SaveState();

            var reader = new StateManager(filePath);
            reader.LoadState();

            var result = reader.GetJobState("jobA");
            Assert.NotNull(result);
            Assert.Equal(StateStatus.Completed, result.Status);
            Assert.Equal(5, result.TotalFiles);
        }

        [Fact]
        public void LoadState_NonExistentFile_DoesNotThrow()
        {
            var filePath = Path.Combine(tempDir, "nonexistent_state.json");
            var manager = new StateManager(filePath);
            var ex = Record.Exception(() => manager.LoadState());
            Assert.Null(ex);
        }

        [Fact]
        public void LoadState_NonExistentFile_GetJobStateReturnsNull()
        {
            var filePath = Path.Combine(tempDir, "nonexistent_state2.json");
            var manager = new StateManager(filePath);
            manager.LoadState();
            Assert.Null(manager.GetJobState("anything"));
        }

        [Fact]
        public void UpdateJobState_CaseInsensitiveLookup_UpdatesExisting()
        {
            var manager = CreateManager();
            manager.UpdateJobState("MyJob", new JobState("MyJob") { TotalFiles = 3 });
            manager.UpdateJobState("myjob", new JobState("myjob") { TotalFiles = 7 });

            // Should update the same entry
            var result = manager.GetJobState("MYJOB");
            Assert.NotNull(result);
            Assert.Equal(7, result.TotalFiles);
        }

        [Fact]
        public void UpdateJobState_ThreadSafe_MultipleConcurrentUpdates()
        {
            var manager = CreateManager();
            var tasks = new List<Task>();

            for (int i = 0; i < 20; i++)
            {
                int idx = i;
                tasks.Add(Task.Run(() =>
                {
                    manager.UpdateJobState($"job{idx}", new JobState($"job{idx}") { TotalFiles = idx });
                }));
            }

            Task.WaitAll(tasks.ToArray());

            for (int i = 0; i < 20; i++)
            {
                var result = manager.GetJobState($"job{i}");
                Assert.NotNull(result);
            }
        }

        [Fact]
        public void SaveAndLoad_PreservesMultipleJobs()
        {
            var filePath = Path.Combine(tempDir, "multi_job_state.json");
            var manager = new StateManager(filePath);
            manager.UpdateJobState("jobA", new JobState("jobA") { Status = StateStatus.Completed });
            manager.UpdateJobState("jobB", new JobState("jobB") { Status = StateStatus.Active, TotalFiles = 10 });
            manager.SaveState();

            var reader = new StateManager(filePath);
            reader.LoadState();

            Assert.Equal(StateStatus.Completed, reader.GetJobState("jobA")!.Status);
            Assert.Equal(StateStatus.Active, reader.GetJobState("jobB")!.Status);
            Assert.Equal(10, reader.GetJobState("jobB")!.TotalFiles);
        }

        public void Dispose()
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }
}

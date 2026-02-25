using EasySave.Core;
using Xunit;

namespace EasySave.Tests
{
    public class JobStateTests
    {
        [Fact]
        public void Constructor_SetsNameAndDefaultValues()
        {
            var state = new JobState("myJob");

            Assert.Equal("myJob", state.Name);
            Assert.Equal(StateStatus.Inactive, state.Status);
            Assert.Equal(0, state.TotalFiles);
            Assert.Equal(0L, state.TotalSize);
            Assert.Equal(0, state.RemainingFiles);
            Assert.Equal(0L, state.RemainingSize);
            Assert.Equal(0.0, state.Progress);
            Assert.Equal(string.Empty, state.CurrentSourceFile);
            Assert.Equal(string.Empty, state.CurrentTargetFile);
        }

        [Fact]
        public void UpdateProgress_AllFilesProcessed_SetsCompletedAnd100Percent()
        {
            var state = new JobState("job") { TotalFiles = 5, TotalSize = 1000 };

            state.UpdateProgress(0, 0);

            Assert.Equal(StateStatus.Completed, state.Status);
            Assert.Equal(100.0, state.Progress);
        }

        [Fact]
        public void UpdateProgress_HalfDone_Returns50Percent()
        {
            var state = new JobState("job") { TotalFiles = 10 };

            state.UpdateProgress(5, 0);

            Assert.Equal(50.0, state.Progress, precision: 1);
            Assert.Equal(StateStatus.Active, state.Status);
        }

        [Fact]
        public void UpdateProgress_NoFilesNoSize_SetsCompleted()
        {
            // When TotalFiles=0 and TotalSize=0, remaining=0 means nothing left → Completed
            var state = new JobState("job") { TotalFiles = 0, TotalSize = 0 };

            state.UpdateProgress(0, 0);

            Assert.Equal(StateStatus.Completed, state.Status);
            Assert.Equal(100.0, state.Progress);
        }

        [Fact]
        public void UpdateProgress_BySizeWhenNoFiles_CalculatesCorrectly()
        {
            var state = new JobState("job") { TotalFiles = 0, TotalSize = 1000 };

            state.UpdateProgress(0, 500);

            Assert.Equal(50.0, state.Progress, precision: 1);
            Assert.Equal(StateStatus.Active, state.Status);
        }

        [Fact]
        public void UpdateProgress_FilesHavePriorityOverSize()
        {
            // When TotalFiles > 0, progress should be based on files, not size
            var state = new JobState("job") { TotalFiles = 4, TotalSize = 1000 };

            state.UpdateProgress(1, 750); // 3 of 4 files done, but 75% by size

            Assert.Equal(75.0, state.Progress, precision: 1); // 3/4 files = 75%
        }

        [Fact]
        public void UpdateProgress_ClampsProgressTo100()
        {
            var state = new JobState("job") { TotalFiles = 5 };

            // Passing negative remaining (shouldn't happen but test robustness)
            state.UpdateProgress(-1, 0);

            Assert.True(state.Progress <= 100.0);
        }

        [Fact]
        public void UpdateProgress_ClampsProgressToMinimum0()
        {
            var state = new JobState("job") { TotalFiles = 5 };

            // More remaining than total (edge case)
            state.UpdateProgress(10, 0);

            Assert.True(state.Progress >= 0.0);
        }

        [Fact]
        public void UpdateProgress_UpdatesTimestamp()
        {
            var state = new JobState("job") { TotalFiles = 10 };
            var before = System.DateTime.UtcNow.AddMilliseconds(-1);

            state.UpdateProgress(5, 0);

            Assert.True(state.Timestamp >= before);
        }

        [Fact]
        public void UpdateProgress_UpdatesRemainingFields()
        {
            var state = new JobState("job") { TotalFiles = 10, TotalSize = 2000 };

            state.UpdateProgress(3, 600);

            Assert.Equal(3, state.RemainingFiles);
            Assert.Equal(600, state.RemainingSize);
        }

        [Fact]
        public void UpdateProgress_OneFileLeft_IsNotCompleted()
        {
            var state = new JobState("job") { TotalFiles = 5 };

            state.UpdateProgress(1, 0);

            Assert.Equal(StateStatus.Active, state.Status);
            Assert.NotEqual(100.0, state.Progress);
        }

        [Fact]
        public void Status_CanBeSetManually()
        {
            var state = new JobState("job");
            state.Status = StateStatus.Error;
            Assert.Equal(StateStatus.Error, state.Status);
        }
    }
}

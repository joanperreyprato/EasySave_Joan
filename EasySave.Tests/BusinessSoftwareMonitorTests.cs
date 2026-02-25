using EasySave.Core;
using Xunit;

namespace EasySave.Tests
{
    public class BusinessSoftwareMonitorTests
    {
        [Fact]
        public void Constructor_Default_EmptyList()
        {
            var monitor = new BusinessSoftwareMonitor();
            Assert.Empty(monitor.GetMonitoredProcesses());
        }

        [Fact]
        public void Constructor_WithProcessNames_InitializesCorrectly()
        {
            var monitor = new BusinessSoftwareMonitor(new[] { "notepad", "calc" });
            Assert.Equal(2, monitor.GetMonitoredProcesses().Count);
        }

        [Fact]
        public void Constructor_WithNullEnumerable_DoesNotThrow()
        {
            var ex = Record.Exception(() => new BusinessSoftwareMonitor(null!));
            Assert.Null(ex);
        }

        [Fact]
        public void AddProcess_NewProcess_AddsToList()
        {
            var monitor = new BusinessSoftwareMonitor();
            monitor.AddProcess("notepad");
            Assert.Contains("notepad", monitor.GetMonitoredProcesses());
        }

        [Fact]
        public void AddProcess_WithExeExtension_StripsExtension()
        {
            var monitor = new BusinessSoftwareMonitor();
            monitor.AddProcess("notepad.exe");
            Assert.Contains("notepad", monitor.GetMonitoredProcesses());
            Assert.DoesNotContain("notepad.exe", monitor.GetMonitoredProcesses());
        }

        [Fact]
        public void AddProcess_Duplicate_NotAddedTwice()
        {
            var monitor = new BusinessSoftwareMonitor();
            monitor.AddProcess("notepad");
            monitor.AddProcess("notepad");
            Assert.Single(monitor.GetMonitoredProcesses());
        }

        [Fact]
        public void AddProcess_DuplicateCaseInsensitive_NotAddedTwice()
        {
            var monitor = new BusinessSoftwareMonitor();
            monitor.AddProcess("Notepad");
            monitor.AddProcess("NOTEPAD");
            Assert.Single(monitor.GetMonitoredProcesses());
        }

        [Fact]
        public void AddProcess_EmptyString_DoesNotAdd()
        {
            var monitor = new BusinessSoftwareMonitor();
            monitor.AddProcess("");
            Assert.Empty(monitor.GetMonitoredProcesses());
        }

        [Fact]
        public void AddProcess_WhitespaceString_DoesNotAdd()
        {
            var monitor = new BusinessSoftwareMonitor();
            monitor.AddProcess("   ");
            Assert.Empty(monitor.GetMonitoredProcesses());
        }

        [Fact]
        public void RemoveProcess_ExistingProcess_RemovesFromList()
        {
            var monitor = new BusinessSoftwareMonitor();
            monitor.AddProcess("notepad");
            monitor.RemoveProcess("notepad");
            Assert.Empty(monitor.GetMonitoredProcesses());
        }

        [Fact]
        public void RemoveProcess_WithExeExtension_RemovesCorrectly()
        {
            var monitor = new BusinessSoftwareMonitor();
            monitor.AddProcess("notepad");
            monitor.RemoveProcess("notepad.exe");
            Assert.Empty(monitor.GetMonitoredProcesses());
        }

        [Fact]
        public void RemoveProcess_NonExistentProcess_DoesNotThrow()
        {
            var monitor = new BusinessSoftwareMonitor();
            var ex = Record.Exception(() => monitor.RemoveProcess("somefakeprocess"));
            Assert.Null(ex);
        }

        [Fact]
        public void ClearProcesses_RemovesAllProcesses()
        {
            var monitor = new BusinessSoftwareMonitor();
            monitor.AddProcess("notepad");
            monitor.AddProcess("calc");
            monitor.AddProcess("excel");
            monitor.ClearProcesses();
            Assert.Empty(monitor.GetMonitoredProcesses());
        }

        [Fact]
        public void GetMonitoredProcesses_ReturnsReadOnlyList()
        {
            var monitor = new BusinessSoftwareMonitor();
            monitor.AddProcess("notepad");
            var list = monitor.GetMonitoredProcesses();
            // Verify it's IReadOnlyList
            Assert.IsAssignableFrom<System.Collections.Generic.IReadOnlyList<string>>(list);
        }

        [Fact]
        public void IsBusinessSoftwareRunning_NoProcessesMonitored_ReturnsFalse()
        {
            var monitor = new BusinessSoftwareMonitor();
            Assert.False(monitor.IsBusinessSoftwareRunning());
        }

        [Fact]
        public void IsBusinessSoftwareRunning_MonitoringNonExistentProcess_ReturnsFalse()
        {
            var monitor = new BusinessSoftwareMonitor();
            monitor.AddProcess("DefinitelyNotRunning_XYZ_12345");
            Assert.False(monitor.IsBusinessSoftwareRunning());
        }

        [Fact]
        public void AddProcess_MultipleDistinctProcesses_AllAdded()
        {
            var monitor = new BusinessSoftwareMonitor();
            monitor.AddProcess("proc1");
            monitor.AddProcess("proc2");
            monitor.AddProcess("proc3");
            Assert.Equal(3, monitor.GetMonitoredProcesses().Count);
        }

        [Fact]
        public void RemoveProcess_EmptyString_DoesNotThrow()
        {
            var monitor = new BusinessSoftwareMonitor();
            monitor.AddProcess("notepad");
            var ex = Record.Exception(() => monitor.RemoveProcess(""));
            Assert.Null(ex);
            Assert.Single(monitor.GetMonitoredProcesses());
        }
    }
}

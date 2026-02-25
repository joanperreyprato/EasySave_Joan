using System;
using System.IO;
using EasyLog;
using Xunit;

namespace EasySave.Tests
{
    public class LogEntryTests
    {
        [Fact]
        public void Constructor_SetsAllProperties()
        {
            var before = DateTime.UtcNow.AddMilliseconds(-1);
            var entry = new LogEntry("backup1", "/src/file.txt", "/dst/file.txt", 1024, 150, LogEntry.EntryType.File);

            Assert.Equal("backup1", entry.BackupName);
            Assert.Equal("/src/file.txt", entry.SourceFilePath);
            Assert.Equal("/dst/file.txt", entry.TargetFilePath);
            Assert.Equal(1024, entry.FileSize);
            Assert.Equal(150, entry.TransferTime);
            Assert.Equal(LogEntry.EntryType.File, entry.Type);
            Assert.Equal(0, entry.EncryptionTime);
            Assert.True(entry.Timestamp >= before);
        }

        [Fact]
        public void Constructor_WithEncryptionTime_SetsEncryptionTime()
        {
            var entry = new LogEntry("backup1", "/src", "/dst", 512, 50, LogEntry.EntryType.File, encryptionTime: 30);
            Assert.Equal(30, entry.EncryptionTime);
        }

        [Fact]
        public void Constructor_SummaryType_SetsTypeCorrectly()
        {
            var entry = new LogEntry("backup1", "/src", "/dst", 4096, 500, LogEntry.EntryType.Summary);
            Assert.Equal(LogEntry.EntryType.Summary, entry.Type);
        }

        [Fact]
        public void Constructor_DefaultEncryptionTime_IsZero()
        {
            var entry = new LogEntry("b", "/s", "/d", 100, 10, LogEntry.EntryType.File);
            Assert.Equal(0, entry.EncryptionTime);
        }

        [Fact]
        public void ToUNCPath_NullPath_ReturnsEmpty()
        {
            var entry = new LogEntry("b", "/s", "/d", 0, 0, LogEntry.EntryType.File);
            Assert.Equal(string.Empty, entry.ToUNCPath(null!));
        }

        [Fact]
        public void ToUNCPath_EmptyPath_ReturnsEmpty()
        {
            var entry = new LogEntry("b", "/s", "/d", 0, 0, LogEntry.EntryType.File);
            Assert.Equal(string.Empty, entry.ToUNCPath(""));
        }

        [Fact]
        public void ToUNCPath_WhitespacePath_ReturnsWhitespace()
        {
            var entry = new LogEntry("b", "/s", "/d", 0, 0, LogEntry.EntryType.File);
            var result = entry.ToUNCPath("   ");
            Assert.Equal("   ", result);
        }

        [Fact]
        public void ToUNCPath_ValidPath_ReturnsNonEmpty()
        {
            var entry = new LogEntry("b", "/s", "/d", 0, 0, LogEntry.EntryType.File);
            string tempFile = Path.GetTempPath();
            var result = entry.ToUNCPath(tempFile);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void LogEntry_NegativeEncryptionTime_AllowedAsErrorCode()
        {
            var entry = new LogEntry("b", "/s", "/d", 100, 10, LogEntry.EntryType.File, encryptionTime: -1);
            Assert.Equal(-1, entry.EncryptionTime);
        }

        [Fact]
        public void LogEntry_ZeroSize_IsValid()
        {
            var entry = new LogEntry("b", "/s", "/d", 0, 0, LogEntry.EntryType.Summary);
            Assert.Equal(0, entry.FileSize);
            Assert.Equal(0, entry.TransferTime);
        }

        [Fact]
        public void LogEntry_Timestamp_IsUtc()
        {
            var entry = new LogEntry("b", "/s", "/d", 100, 10, LogEntry.EntryType.File);
            Assert.Equal(DateTimeKind.Utc, entry.Timestamp.Kind);
        }
    }
}

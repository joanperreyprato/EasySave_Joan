using EasyLog;

namespace EasySave.Tests;

/// <summary>
/// A "spy" implementation of ILogger.
/// It only records whether the Log method was called during the test.
/// </summary>
public class SpyLogger : ILogger
{
    public int LogCallCount { get; private set; } = 0;
    public bool ShouldThrow { get; set; } = false;

    public void Log(LogEntry entry)
    {
        if (ShouldThrow) 
            throw new Exception("Simulated network failure");
            
        LogCallCount++;
    }
}

public class CompositeLoggerTests
{
    [Fact]
    public void Log_Should_Distribute_To_All_Inner_Loggers()
    {
        // Arrange
        var spy1 = new SpyLogger();
        var spy2 = new SpyLogger();
        var composite = new CompositeLogger(new ILogger[] { spy1, spy2 });
        var entry = new LogEntry
        {
            BackupName = "test",
            SourceFilePath = "source",
            TargetFilePath = "target",
            FileSize = 100,
            TransferDurationMs = 10,
            EncryptionDurationMs = 5
        };

        // Act
        composite.Log(entry);

        // Assert
        Assert.Equal(1, spy1.LogCallCount);
        Assert.Equal(1, spy2.LogCallCount);
    }

    [Fact]
    public void Log_Should_Not_Crash_If_One_Logger_Fails()
    {
        // Arrange
        var workingSpy = new SpyLogger();
        var failingSpy = new SpyLogger { ShouldThrow = true };
        
        // The failing logger comes first, to check that the second one is still called
        var composite = new CompositeLogger(new ILogger[] { failingSpy, workingSpy });
        var entry = new LogEntry
        {
            BackupName = "test",
            SourceFilePath = "source",
            TargetFilePath = "target",
            FileSize = 100,
            TransferDurationMs = 10,
            EncryptionDurationMs = 5
        };

        // Act
        // The test passes only if no exception is thrown
        composite.Log(entry);

        // Assert
        Assert.Equal(1, workingSpy.LogCallCount); // The second logger received the entry
    }
}

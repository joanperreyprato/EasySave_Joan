using EasyLog;

namespace EasySave.Tests;

/// <summary>Unit tests for <see cref="JsonLogger"/>.</summary>
public sealed class JsonLoggerTests : IDisposable
{
    private readonly string _logDir;
    private readonly JsonLogger _logger;

    public JsonLoggerTests()
    {
        _logDir = Path.Combine(Path.GetTempPath(), $"es_log_{Guid.NewGuid():N}");
        _logger = new JsonLogger(_logDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_logDir)) Directory.Delete(_logDir, recursive: true);
    }

    [Fact]
    public void Log_CreatesValidJsonFile()
    {
        var entry = new LogEntry
        {
            BackupName = "Test",
            SourceFilePath = @"C:\source\file.txt",
            TargetFilePath = @"C:\target\file.txt",
            FileSize = 1024,
            TransferDurationMs = 42
        };

        _logger.Log(entry);

        var files = Directory.GetFiles(_logDir, "*.json");
        Assert.Single(files);

        var content = File.ReadAllText(files[0]);
        // Must be a valid JSON array
        var array = System.Text.Json.JsonDocument.Parse(content);
        Assert.Equal(System.Text.Json.JsonValueKind.Array, array.RootElement.ValueKind);
        Assert.Equal(1, array.RootElement.GetArrayLength());
    }

    [Fact]
    public void Log_MultipleEntries_ProducesValidJsonArray()
    {
        for (int i = 0; i < 5; i++)
            _logger.Log(new LogEntry { BackupName = $"Job{i}", FileSize = i * 100 });

        var files = Directory.GetFiles(_logDir, "*.json");
        Assert.Single(files);

        var content = File.ReadAllText(files[0]);
        var array = System.Text.Json.JsonDocument.Parse(content);
        Assert.Equal(System.Text.Json.JsonValueKind.Array, array.RootElement.ValueKind);
        Assert.Equal(5, array.RootElement.GetArrayLength());
    }

    [Fact]
    public void Log_PathConvertedToUnc()
    {
        _logger.Log(new LogEntry { SourceFilePath = @"C:\data\file.txt" });

        var files = Directory.GetFiles(_logDir, "*.json");
        var content = File.ReadAllText(files[0]);
        Assert.Contains(@"\\", content);
    }
}

/// <summary>Unit tests for <see cref="XmlLogger"/>.</summary>
public sealed class XmlLoggerTests : IDisposable
{
    private readonly string _logDir;
    private readonly XmlLogger _logger;

    public XmlLoggerTests()
    {
        _logDir = Path.Combine(Path.GetTempPath(), $"es_xmllog_{Guid.NewGuid():N}");
        _logger = new XmlLogger(_logDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_logDir)) Directory.Delete(_logDir, recursive: true);
    }

    [Fact]
    public void Log_CreatesValidXmlFile()
    {
        _logger.Log(new LogEntry { BackupName = "XmlTest", FileSize = 512 });

        var files = Directory.GetFiles(_logDir, "*.xml");
        Assert.Single(files);

        // Must be parseable as valid XML
        var doc = new System.Xml.XmlDocument();
        doc.Load(files[0]);

        Assert.Equal("Logs", doc.DocumentElement!.Name);
        Assert.Equal(1, doc.DocumentElement.ChildNodes.Count);
    }

    [Fact]
    public void Log_MultipleEntries_AllInRoot()
    {
        for (int i = 0; i < 3; i++)
            _logger.Log(new LogEntry { BackupName = $"Job{i}" });

        var files = Directory.GetFiles(_logDir, "*.xml");
        var doc = new System.Xml.XmlDocument();
        doc.Load(files[0]);
        Assert.Equal(3, doc.DocumentElement!.ChildNodes.Count);
    }
}

/// <summary>Unit tests for <see cref="LogEntry.ToUncPath"/>.</summary>
public sealed class LogEntryTests
{
    [Theory]
    [InlineData(@"C:\folder\file.txt")]
    [InlineData(@"D:\data\backup\file.docx")]
    public void ToUncPath_LocalPath_ReturnsUncFormat(string local)
    {
        var unc = LogEntry.ToUncPath(local);
        Assert.StartsWith(@"\\", unc);
        Assert.Contains(Environment.MachineName, unc);
    }

    [Fact]
    public void ToUncPath_AlreadyUnc_ReturnsSame()
    {
        var unc = @"\\server\share\file.txt";
        Assert.Equal(unc, LogEntry.ToUncPath(unc));
    }

    [Fact]
    public void ToUncPath_Empty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, LogEntry.ToUncPath(string.Empty));
    }
}

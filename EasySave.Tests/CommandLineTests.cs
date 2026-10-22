using EasySave.CLI;
using EasySave.Core.Models;
using EasySave.Core.Services;

namespace EasySave.Tests;

/// <summary>Unit tests for <see cref="JobSelectionParser"/> ("1-3", "1;3"...).</summary>
public sealed class JobSelectionParserTests
{
    [Theory]
    [InlineData("1-3", 5, new[] { 1, 2, 3 })]
    [InlineData("1;3", 5, new[] { 1, 3 })]
    [InlineData("1,3", 5, new[] { 1, 3 })]
    [InlineData("3 1 3", 5, new[] { 1, 3 })]
    [InlineData("2-9", 4, new[] { 2, 3, 4 })]
    [InlineData("all", 3, new[] { 1, 2, 3 })]
    [InlineData("0;7;abc;3-1", 5, new int[0])]
    public void Parse_ReturnsSortedValidJobNumbers(string input, int jobCount, int[] expected)
        => Assert.Equal(expected, JobSelectionParser.Parse(input, jobCount));
}

/// <summary>
/// End-to-end tests of the v3.0 command line ("EasySave.exe 1-3"): it runs the v3.0 jobs with the v3.0 rules.
/// </summary>
public sealed class CommandLineTests : TempWorkspace
{
    private readonly StateManager _stateManager;
    private readonly AppSettings _settings;
    private readonly BackupServiceV3 _service;
    private readonly ConsoleMenu _menu;

    public CommandLineTests()
    {
        _stateManager = new StateManager(NewFile());
        _settings = new AppSettings(NewFile());
        _service = new BackupServiceV3(_stateManager, _settings, new BusinessSoftwareMonitor(), NewFile());
        _menu = new ConsoleMenu(_service, _stateManager, LanguageProvider.Instance, _settings, logDirectory: NewDir());
    }

    private string CreateJob(string name, params string[] files)
    {
        string src = NewDir();
        WriteFiles(src, files);
        string target = Path.Combine(NewDir(), name);
        Assert.Equal(BackupCreationResult.Success, _service.CreateJob(name, src, target, BackupType.Full));
        return target;
    }

    [Fact]
    public void Range_RunsOnlyTheSelectedJobs_AndReturnsZero()
    {
        string t1 = CreateJob("Cli1", "a.txt");
        string t2 = CreateJob("Cli2", "b.txt");
        string t3 = CreateJob("Cli3", "c.txt");

        int exitCode = _menu.ExecuteFromArgs(["1-2"]);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(t1, "a.txt")));
        Assert.True(File.Exists(Path.Combine(t2, "b.txt")));
        Assert.False(Directory.Exists(t3));
    }

    [Fact]
    public void MoreThanFiveJobs_CanBeRunFromTheCommandLine()
    {
        var targets = Enumerable.Range(1, 6).Select(i => CreateJob($"Job{i}", $"f{i}.txt")).ToList();

        Assert.Equal(0, _menu.ExecuteFromArgs(["6"]));
        Assert.True(File.Exists(Path.Combine(targets[5], "f6.txt")));
    }

    [Fact]
    public void CommandLine_AppliesTheV3Rules_Encryption()
    {
        _settings.EncryptionExtensions = [".txt"];
        string target = CreateJob("Encrypted", "secret.txt");

        Assert.Equal(0, _menu.ExecuteFromArgs(["1"]));

        string copy = File.ReadAllText(Path.Combine(target, "secret.txt"));
        Assert.NotEqual("content of secret.txt", copy); // Encrypted by CryptoSoft
    }

    [Fact]
    public void InvalidSelection_ReturnsOne()
    {
        CreateJob("Only", "a.txt");

        Assert.Equal(1, _menu.ExecuteFromArgs(["7"]));
    }
}

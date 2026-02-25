using EasySave.Core;
using Xunit;

namespace EasySave.Tests
{
    public class CommandLineParserTests
    {
        [Fact]
        public void ParseArguments_NullArray_ReturnsEmpty()
        {
            var result = CommandLineParser.ParseArguments(null!);
            Assert.Empty(result);
        }

        [Fact]
        public void ParseArguments_EmptyArray_ReturnsEmpty()
        {
            var result = CommandLineParser.ParseArguments(System.Array.Empty<string>());
            Assert.Empty(result);
        }

        [Fact]
        public void ParseArguments_SingleIndex_ReturnsSingleItem()
        {
            var result = CommandLineParser.ParseArguments(new[] { "3" });
            Assert.Single(result);
            Assert.Equal(3, result[0]);
        }

        [Fact]
        public void ParseArguments_CommaSeparated_ReturnsMultiple()
        {
            var result = CommandLineParser.ParseArguments(new[] { "1,3,5" });
            Assert.Equal(new[] { 1, 3, 5 }, result);
        }

        [Fact]
        public void ParseArguments_Range_ReturnsAllInRange()
        {
            var result = CommandLineParser.ParseArguments(new[] { "2-5" });
            Assert.Equal(new[] { 2, 3, 4, 5 }, result);
        }

        [Fact]
        public void ParseArguments_SemicolonSeparated_ReturnsMultiple()
        {
            var result = CommandLineParser.ParseArguments(new[] { "1;2;3" });
            Assert.Equal(new[] { 1, 2, 3 }, result);
        }

        [Fact]
        public void ParseArguments_SkipsKnownCommands()
        {
            var result = CommandLineParser.ParseArguments(new[] { "execute", "1", "2" });
            Assert.Equal(new[] { 1, 2 }, result);
        }

        [Fact]
        public void ParseArguments_Deduplicates()
        {
            var result = CommandLineParser.ParseArguments(new[] { "1,1,2" });
            Assert.Equal(new[] { 1, 2 }, result);
        }

        [Fact]
        public void ParseArguments_SortsAscending()
        {
            var result = CommandLineParser.ParseArguments(new[] { "5,2,8,1" });
            Assert.Equal(new[] { 1, 2, 5, 8 }, result);
        }

        [Fact]
        public void ParseArguments_MixedRangeAndSingle_ParsesAll()
        {
            var result = CommandLineParser.ParseArguments(new[] { "1-3,5" });
            Assert.Equal(new[] { 1, 2, 3, 5 }, result);
        }

        [Fact]
        public void ParseArguments_WhitespaceOnly_ReturnsEmpty()
        {
            var result = CommandLineParser.ParseArguments(new[] { "   " });
            Assert.Empty(result);
        }

        [Fact]
        public void ParseArguments_MultipleArgs_CombinesAll()
        {
            var result = CommandLineParser.ParseArguments(new[] { "1", "3", "5" });
            Assert.Equal(new[] { 1, 3, 5 }, result);
        }

        [Theory]
        [InlineData("create")]
        [InlineData("execute")]
        [InlineData("list")]
        [InlineData("help")]
        [InlineData("all")]
        [InlineData("full")]
        [InlineData("diff")]
        [InlineData("delete")]
        public void IsValidCommand_KnownCommands_ReturnsTrue(string command)
        {
            Assert.True(CommandLineParser.IsValidCommand(command));
        }

        [Theory]
        [InlineData("CREATE")]
        [InlineData("EXECUTE")]
        [InlineData("List")]
        public void IsValidCommand_CaseInsensitive_ReturnsTrue(string command)
        {
            Assert.True(CommandLineParser.IsValidCommand(command));
        }

        [Fact]
        public void IsValidCommand_NullArg_ReturnsFalse()
        {
            Assert.False(CommandLineParser.IsValidCommand(null!));
        }

        [Fact]
        public void IsValidCommand_EmptyArg_ReturnsFalse()
        {
            Assert.False(CommandLineParser.IsValidCommand(""));
        }

        [Fact]
        public void IsValidCommand_UnknownCommand_ReturnsFalse()
        {
            Assert.False(CommandLineParser.IsValidCommand("run"));
            Assert.False(CommandLineParser.IsValidCommand("backup"));
        }

        [Fact]
        public void IsValidCommand_WithDashPrefix_ReturnsTrue()
        {
            Assert.True(CommandLineParser.IsValidCommand("--create"));
            Assert.True(CommandLineParser.IsValidCommand("-execute"));
        }

        [Fact]
        public void ParseArguments_RangeReversed_StillParsesCorrectly()
        {
            // 5-2 should produce 2,3,4,5
            var result = CommandLineParser.ParseArguments(new[] { "5-2" });
            Assert.Equal(new[] { 2, 3, 4, 5 }, result);
        }
    }
}

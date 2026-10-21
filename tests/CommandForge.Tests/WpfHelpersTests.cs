using System.Collections.Specialized;
using CommandForge.Application.Settings;
using CommandForge.Application.UserCommands;
using CommandForge.Desktop;
using CommandForge.Desktop.ViewModels;
using CommandForge.Domain;
using CommandForge.Infrastructure;
using CommandForge.Infrastructure.Portability;

namespace CommandForge.Tests;

/// <summary>Tests the console line buffer, crash support codes, user-command building and settings import.</summary>
public sealed class WpfHelpersTests
{
    [Fact]
    public void OutputBuffer_KeepsTheNewestLinesAndCountsTheRest()
    {
        var buffer = new OutputBuffer(capacity: 100);

        buffer.Append(Enumerable.Range(0, 250).Select(i => new OutputLine($"line {i}", false)).ToList());

        Assert.True(buffer.Count <= 100);
        Assert.Equal(250, buffer.Count + buffer.DroppedCount);
        Assert.Equal("line 249", buffer[^1].Text);
    }

    [Fact]
    public void OutputBuffer_RaisesOneNotificationPerBatch()
    {
        var buffer = new OutputBuffer();
        var notifications = 0;
        ((INotifyCollectionChanged)buffer).CollectionChanged += (_, _) => notifications++;

        buffer.Append(Enumerable.Range(0, 500).Select(i => new OutputLine(i.ToString(System.Globalization.CultureInfo.InvariantCulture), false)).ToList());
        buffer.Add(new OutputLine("one more", true));

        Assert.Equal(2, notifications);
        Assert.Equal(501, buffer.Count);
    }

    [Fact]
    public void OutputBuffer_ClearResetsTheDroppedCount()
    {
        var buffer = new OutputBuffer(capacity: 10);
        buffer.Append(Enumerable.Range(0, 30).Select(i => new OutputLine("x", false)).ToList());

        buffer.Clear();

        Assert.Empty(buffer);
        Assert.Equal(0, buffer.DroppedCount);
        Assert.Equal(string.Empty, buffer.ToText());
    }

    [Fact]
    public void CrashCode_IsStableForTheSameExceptionType()
    {
        // The suffix identifies the failure across runs (unlike string.GetHashCode, randomized per process).
        var first = CrashHandlingService.GenerateCode(new InvalidOperationException("a"));
        var second = CrashHandlingService.GenerateCode(new InvalidOperationException("b"));
        var other = CrashHandlingService.GenerateCode(new IOException());

        Assert.Equal(first[^4..], second[^4..]);
        Assert.NotEqual(first[^4..], other[^4..]);
        Assert.Matches("^[0-9]{6}-[0-9]{6}-[0-9A-F]{4}$", first);
    }

    [Theory]
    [InlineData("\"cmd.exe\"", "dir", "/c dir")]
    [InlineData("cmd", "/q dir", "/c /q dir")]
    [InlineData("cmd", "echo /c", "/c echo /c")]
    [InlineData("cmd", "/q /c dir", "/q /c dir")]
    public void UserCommand_CmdNeedsARunSwitchAmongItsLeadingSwitches(string executable, string arguments, string expected)
        => Assert.Equal(expected, UserCommandFactory.NormalizeArguments(executable, arguments));

    [Fact]
    public void UserCommand_IsNeverElevatedAndNeverUsesResourceKeys()
    {
        var definition = UserCommandFactory.ToDefinition(new UserCommand { Id = "AppTitle", Name = "AppTitle", Executable = "ping", Arguments = "1.1.1.1" });

        Assert.False(definition.RequiresAdmin);
        Assert.Equal("user:AppTitle", definition.Id);
        Assert.Equal("user:AppTitle", definition.TitleKey); // a name equal to a resource key stays a name
        Assert.Equal(DangerLevel.Caution, definition.DangerLevel);
    }

    [Fact]
    public void SettingsImport_RepairsNullsAndUnknownValues()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"cf-import-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var import = Path.Combine(directory, "settings.json");
            File.WriteAllText(import, """{ "theme": 7, "language": null, "favoriteCommandIds": null, "logLevel": "Warning" }""");
            var settings = new JsonSettingsService(Path.Combine(directory, "config.json"));

            var portable = new JsonSettingsFile().Read(import);
            Assert.NotNull(portable);
            SettingsTransfer.Apply(portable, settings);

            Assert.Equal(AppTheme.System, settings.Theme);
            Assert.Equal(string.Empty, settings.Language);
            Assert.Empty(settings.FavoriteCommandIds);
            Assert.Equal(CommandForge.Application.Logging.LogLevel.Warning, settings.LogLevel);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void SettingsImport_InvalidFileIsLeftUntouched()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cf-import-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{ not json");
        try
        {
            Assert.Null(new JsonSettingsFile().Read(path));
            Assert.Equal("{ not json", File.ReadAllText(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}

using CommandForge.Application.UserCommands;
using CommandForge.Domain;

namespace CommandForge.Tests;

/// <summary>
/// Tests the cmd argument normalization that fixes user commands hanging on an interactive
/// cmd.exe (e.g. "cmd" + "ping google.com" must run as "cmd /c ping google.com").
/// </summary>
public sealed class UserCommandNormalizationTests
{
    [Theory]
    [InlineData("cmd", "ping google.com", "/c ping google.com")]
    [InlineData("cmd.exe", "dir", "/c dir")]
    [InlineData("CMD", "echo hi", "/c echo hi")]
    [InlineData("C:\\Windows\\System32\\cmd.exe", "whoami", "/c whoami")]
    [InlineData("cmd", "", "/c")]
    public void NormalizeArguments_PrependsSlashC_ForCmd(string exe, string args, string expected)
        => Assert.Equal(expected, UserCommandFactory.NormalizeArguments(exe, args));

    [Theory]
    [InlineData("cmd", "/c ping x")]      // already has /c
    [InlineData("cmd", "/k keep open")]   // explicit /k
    [InlineData("cmd", "/C UPPER")]       // case-insensitive switch
    [InlineData("powershell", "ping x")]  // not cmd — leave as-is (powershell runs it)
    [InlineData("ping", "google.com")]    // direct executable
    public void NormalizeArguments_LeavesAsIs_WhenNotPlainCmd(string exe, string args)
        => Assert.Equal(args, UserCommandFactory.NormalizeArguments(exe, args));

    [Fact]
    public void ToDefinition_AppliesCmdNormalization()
    {
        var def = UserCommandFactory.ToDefinition(new UserCommand
        {
            Id = "x",
            Name = "n",
            Executable = "cmd",
            Arguments = "ping google.com",
        });

        Assert.Equal("/c ping google.com", def.ArgsTemplate);
    }
}

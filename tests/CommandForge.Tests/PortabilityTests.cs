using CommandForge.Application.Logging;
using CommandForge.Application.Settings;
using CommandForge.Infrastructure.Portability;

namespace CommandForge.Tests;

/// <summary>Verifies import/export files and the named-profile store round-trip and fall back gracefully.</summary>
public sealed class PortabilityTests
{
    private static PortableSettings Sample() => new()
    {
        Theme = AppTheme.Dark,
        Language = "vi",
        FontSize = FontScale.Large,
        ConfirmCaution = false,
        LogLevel = LogLevel.Debug,
        FavoriteCommandIds = ["a", "b"],
    };

    [Fact]
    public void SettingsFile_Write_Then_Read_RoundTrips()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cf-export-{Guid.NewGuid():N}.json");
        try
        {
            var file = new JsonSettingsFile();
            file.Write(path, Sample());

            var read = file.Read(path);

            Assert.NotNull(read);
            Assert.Equal(AppTheme.Dark, read!.Theme);
            Assert.Equal("vi", read.Language);
            Assert.Equal(FontScale.Large, read.FontSize);
            Assert.False(read.ConfirmCaution);
            Assert.Equal(["a", "b"], read.FavoriteCommandIds);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SettingsFile_Read_MissingOrInvalid_ReturnsNull()
    {
        var file = new JsonSettingsFile();
        Assert.Null(file.Read(Path.Combine(Path.GetTempPath(), $"cf-missing-{Guid.NewGuid():N}.json")));

        var bad = Path.Combine(Path.GetTempPath(), $"cf-bad-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(bad, "{ not json ");
            Assert.Null(file.Read(bad));
        }
        finally
        {
            File.Delete(bad);
        }
    }

    [Fact]
    public void ProfileService_Save_Get_Delete_Persists()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cf-profiles-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new JsonProfileService(path);
            svc.Save("Gaming PC", Sample());
            svc.Save("Office", new PortableSettings { Theme = AppTheme.Light });

            Assert.Equal(["Gaming PC", "Office"], svc.GetNames());
            Assert.Equal(AppTheme.Dark, svc.Get("Gaming PC")!.Theme);

            // Reload from disk — profiles persist.
            var reloaded = new JsonProfileService(path);
            Assert.Equal(2, reloaded.GetNames().Count);
            Assert.Equal(AppTheme.Light, reloaded.Get("Office")!.Theme);
            Assert.Equal(["a", "b"], reloaded.Get("Gaming PC")!.FavoriteCommandIds);

            reloaded.Delete("Office");
            Assert.Equal(["Gaming PC"], reloaded.GetNames());
            Assert.Null(new JsonProfileService(path).Get("Office"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ProfileService_Save_OverwritesByName_CaseInsensitive()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cf-profiles-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new JsonProfileService(path);
            svc.Save("Office", new PortableSettings { Theme = AppTheme.Light });
            svc.Save("office", new PortableSettings { Theme = AppTheme.Dark });

            Assert.Single(svc.GetNames());
            Assert.Equal(AppTheme.Dark, svc.Get("Office")!.Theme);
        }
        finally
        {
            File.Delete(path);
        }
    }
}

using CommandForge.Application;
using CommandForge.Application.Logging;
using CommandForge.Application.Ports;
using CommandForge.Application.Settings;

namespace CommandForge.Tests;

/// <summary>Tests for the first-run legal acceptance logic.</summary>
public sealed class LegalGateServiceTests
{
    [Fact]
    public void HasAcceptedCurrentTerms_IsFalse_WhenNothingAccepted()
    {
        var settings = new FakeSettings();
        var sut = new LegalGateService(settings);

        Assert.False(sut.HasAcceptedCurrentTerms());
    }

    [Fact]
    public void AcceptCurrentTerms_RecordsAndPersistsCurrentVersion()
    {
        var settings = new FakeSettings();
        var sut = new LegalGateService(settings);

        sut.AcceptCurrentTerms();

        Assert.Equal(LegalGateService.CurrentTermsVersion, settings.AcceptedTermsVersion);
        Assert.True(sut.HasAcceptedCurrentTerms());
        Assert.Equal(1, settings.SaveCount);
    }

    [Fact]
    public void HasAcceptedCurrentTerms_IsFalse_WhenOlderVersionAccepted()
    {
        var settings = new FakeSettings { AcceptedTermsVersion = "0.0" };
        var sut = new LegalGateService(settings);

        Assert.False(sut.HasAcceptedCurrentTerms());
    }

    private sealed class FakeSettings : ISettingsService
    {
        public string? AcceptedTermsVersion { get; set; }

        public bool AutoCheckForUpdates { get; set; } = true;

        public AppTheme Theme { get; set; } = AppTheme.System;

        public string Language { get; set; } = "";

        public FontScale FontSize { get; set; } = FontScale.Medium;

        public bool CollapseSidebarByDefault { get; set; }

        public bool ShowAdminRestartBadges { get; set; } = true;

        public bool ConfirmCaution { get; set; } = true;

        public bool AutoCreateRestorePoint { get; set; } = true;

        public bool AutoScrollConsole { get; set; } = true;

        public bool WarnOnCancel { get; set; } = true;

        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        public IReadOnlyList<string> FavoriteCommandIds { get; set; } = [];

        public IReadOnlyList<string> RecentCommandIds { get; set; } = [];

        public double? WindowWidth { get; set; }

        public double? WindowHeight { get; set; }

        public double? WindowLeft { get; set; }

        public double? WindowTop { get; set; }

        public bool WindowMaximized { get; set; }

        public int SaveCount { get; private set; }

        public void Save() => SaveCount++;
    }
}

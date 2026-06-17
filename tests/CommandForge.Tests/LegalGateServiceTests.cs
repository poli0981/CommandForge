using CommandForge.Application;
using CommandForge.Application.Ports;

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

        public int SaveCount { get; private set; }

        public void Save() => SaveCount++;
    }
}

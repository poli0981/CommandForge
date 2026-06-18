using CommandForge.Application.Ports;

namespace CommandForge.Application;

/// <summary>
/// Decides whether the user has accepted the current legal terms (EULA / GPLv3 /
/// Disclaimer / Privacy) and records acceptance. Pure application logic; persistence
/// is delegated to <see cref="ISettingsService"/>.
/// </summary>
public sealed class LegalGateService(ISettingsService settings)
{
    /// <summary>
    /// The current terms version. Bump this when the legal terms change so the gate
    /// is shown again.
    /// </summary>
    public const string CurrentTermsVersion = "1.1";

    /// <summary>Whether the user has already accepted the current terms version.</summary>
    public bool HasAcceptedCurrentTerms()
        => string.Equals(settings.AcceptedTermsVersion, CurrentTermsVersion, StringComparison.Ordinal);

    /// <summary>Records acceptance of the current terms version and persists it.</summary>
    public void AcceptCurrentTerms()
    {
        settings.AcceptedTermsVersion = CurrentTermsVersion;
        settings.Save();
    }
}

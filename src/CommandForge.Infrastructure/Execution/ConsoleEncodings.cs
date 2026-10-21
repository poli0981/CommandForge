using System.Text;
using Windows.Win32;

namespace CommandForge.Infrastructure.Execution;

/// <summary>The code page native console tools use for redirected output.</summary>
internal static class ConsoleEncodings
{
    /// <summary>The OEM code page (437 on US English, 1258 on Vietnamese, 932 on Japanese, 936 on Simplified Chinese…).</summary>
    public static Encoding Oem { get; } = CreateOem();

    private static Encoding CreateOem()
    {
        // .NET only ships UTF/ASCII/Latin-1 by default; the legacy code pages come from this provider.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        try
        {
            return Encoding.GetEncoding((int)PInvoke.GetOEMCP());
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            return Encoding.Latin1;
        }
    }
}

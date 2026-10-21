using CommandForge.Infrastructure;

namespace CommandForge.Tests;

/// <summary>
/// Verifies the single-instance detection and activation-signalling logic. Each test uses a unique
/// name suffix so the named kernel objects never collide with a real running instance or with each
/// other. The window activation itself lives in the WPF layer and is verified manually.
/// </summary>
public sealed class SingleInstanceGuardTests
{
    [Fact]
    public void FirstInstanceIsPrimary_SecondIsNot()
    {
        var name = Guid.NewGuid().ToString("N");

        using var first = new SingleInstanceGuard(name);
        using var second = new SingleInstanceGuard(name);

        Assert.True(first.IsPrimaryInstance);
        Assert.False(second.IsPrimaryInstance);
    }

    [Fact]
    public void SignalPrimaryInstance_InvokesPrimaryCallback()
    {
        var name = Guid.NewGuid().ToString("N");
        using var activated = new ManualResetEventSlim(initialState: false);

        using var primary = new SingleInstanceGuard(name);
        primary.StartListening(() => activated.Set());

        using (var secondary = new SingleInstanceGuard(name))
        {
            Assert.False(secondary.IsPrimaryInstance);
            secondary.SignalPrimaryInstance();
        }

        Assert.True(activated.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Name_IsReclaimed_AfterPrimaryDisposed()
    {
        var name = Guid.NewGuid().ToString("N");

        var first = new SingleInstanceGuard(name);
        Assert.True(first.IsPrimaryInstance);
        first.Dispose();

        using var second = new SingleInstanceGuard(name);
        Assert.True(second.IsPrimaryInstance);
    }
}

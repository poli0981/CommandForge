using CommandForge.Infrastructure;
using Velopack;

namespace CommandForge.Desktop;

/// <summary>
/// Process entry point. Order matters: Velopack's install/update/uninstall hooks run first (they
/// may exit the process and must not wait for WPF to load App.xaml resources), then the
/// single-instance check, and only then is the WPF <see cref="App"/> created.
/// </summary>
public static class Program
{
    [STAThread]
    public static int Main()
    {
        VelopackApp.Build().Run();

        using var guard = new SingleInstanceGuard();
        if (!guard.IsPrimaryInstance)
        {
            // A later launch only asks the running instance to come to the front, then exits.
            guard.SignalPrimaryInstance();
            return 0;
        }

        var app = new App(guard);
        app.InitializeComponent();
        return app.Run();
    }
}

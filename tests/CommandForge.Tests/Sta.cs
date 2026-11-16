using System.Runtime.ExceptionServices;
using System.Windows.Threading;

namespace CommandForge.Tests;

/// <summary>
/// Runs a test's body on a thread of its own in a single-threaded apartment, where WPF elements can
/// be created, and rethrows what it threw. No window is shown.
/// </summary>
internal static class Sta
{
    public static void Run(Action body)
    {
        ArgumentNullException.ThrowIfNull(body);
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                body();
            }
            catch (Exception ex)
            {
                failure = ExceptionDispatchInfo.Capture(ex);
            }
            finally
            {
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}

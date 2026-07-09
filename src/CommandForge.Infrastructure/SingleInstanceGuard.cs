namespace CommandForge.Infrastructure;

/// <summary>
/// Enforces a single running application instance per user session. The first instance owns a
/// named <see cref="Mutex"/>; later instances detect it, signal the primary (via a named
/// auto-reset event) to bring its window to the foreground, and then exit.
/// </summary>
/// <remarks>
/// Intentionally WPF- and logging-free so it can be constructed before the host/UI are built and
/// unit-tested without a UI thread. The named objects are session-scoped (<c>Local\</c>), which is
/// correct for a per-user install: it isolates different Windows users / RDP sessions and
/// self-recovers if a process crashes (the kernel object dies when its last handle closes).
/// </remarks>
public sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activateEvent;
    private readonly ManualResetEventSlim _stopEvent = new(initialState: false);
    private Thread? _listener;
    private bool _disposed;

    /// <summary>
    /// Creates the guard, attempting to acquire the single-instance mutex for this session.
    /// </summary>
    /// <param name="nameSuffix">
    /// Optional suffix that isolates the named objects (used by tests to avoid colliding with a
    /// real running instance). When omitted, the shared application name is used so every real
    /// instance contends on the same objects.
    /// </param>
    public SingleInstanceGuard(string? nameSuffix = null)
    {
        var baseName = string.IsNullOrEmpty(nameSuffix)
            ? AppPaths.AppFolderName
            : $"{AppPaths.AppFolderName}.{nameSuffix}";

        // Create the activation event FIRST and hold its handle for the process lifetime: by the
        // time the mutex exists the primary already owns a live handle to the event, so a second
        // instance can neither lose its signal nor destroy the kernel object when it disposes.
        _activateEvent = new EventWaitHandle(
            initialState: false,
            EventResetMode.AutoReset,
            $"Local\\{baseName}.SingleInstance.Activate");

        _mutex = new Mutex(initiallyOwned: false, $"Local\\{baseName}.SingleInstance", out var createdNew);
        IsPrimaryInstance = createdNew;
    }

    /// <summary>True when this is the first (owning) instance for the current session.</summary>
    public bool IsPrimaryInstance { get; }

    /// <summary>
    /// Primary instance only: starts a background listener that invokes
    /// <paramref name="onActivateRequested"/> whenever another instance signals. The callback runs
    /// on a background thread; the caller is responsible for marshalling to the UI thread.
    /// </summary>
    public void StartListening(Action onActivateRequested)
    {
        ArgumentNullException.ThrowIfNull(onActivateRequested);
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_listener is not null)
        {
            throw new InvalidOperationException("The activation listener has already been started.");
        }

        _listener = new Thread(() => ListenLoop(onActivateRequested))
        {
            IsBackground = true,
            Name = "SingleInstanceActivationListener",
        };
        _listener.Start();
    }

    /// <summary>Secondary instance only: asks the primary instance to activate its window.</summary>
    public void SignalPrimaryInstance() => _activateEvent.Set();

    private void ListenLoop(Action onActivateRequested)
    {
        var handles = new WaitHandle[] { _activateEvent, _stopEvent.WaitHandle };
        while (true)
        {
            if (WaitHandle.WaitAny(handles) == 1)
            {
                return; // Stop requested by Dispose().
            }

            try
            {
                onActivateRequested();
            }
            catch (Exception)
            {
                // A faulty activation callback must never tear down the listener thread.
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _stopEvent.Set();
        if (_listener is { IsAlive: true } listener)
        {
            listener.Join(TimeSpan.FromSeconds(2));
        }

        // The mutex is never owned (initiallyOwned: false), so there is nothing to release —
        // closing the handle frees the name once the last instance exits.
        _mutex.Dispose();
        _activateEvent.Dispose();
        _stopEvent.Dispose();
    }
}

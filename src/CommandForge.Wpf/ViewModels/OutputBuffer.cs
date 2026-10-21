using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using CommandForge.Domain;

namespace CommandForge.Wpf.ViewModels;

/// <summary>
/// The console's line store. Lines arrive in batches and raise one notification per batch (not
/// one per line), and only the newest <see cref="Capacity"/> lines are kept — older ones are dropped
/// in chunks and counted in <see cref="DroppedCount"/> — so a command printing hundreds of thousands
/// of lines can neither flood the dispatcher nor exhaust memory. Implements <see cref="IList"/> so
/// WPF binds it with an index-based (virtualizing-friendly) collection view.
/// </summary>
public sealed class OutputBuffer : IReadOnlyList<OutputLine>, IList, INotifyCollectionChanged, INotifyPropertyChanged
{
    /// <summary>Default number of lines kept for display, copy and save.</summary>
    public const int DefaultCapacity = 10_000;

    private readonly List<OutputLine> _lines = [];

    public OutputBuffer(int capacity = DefaultCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 10);
        Capacity = capacity;
    }

    /// <inheritdoc />
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Maximum number of lines kept.</summary>
    public int Capacity { get; }

    /// <summary>How many of the oldest lines were dropped to stay within <see cref="Capacity"/>.</summary>
    public int DroppedCount { get; private set; }

    /// <inheritdoc cref="IReadOnlyCollection{T}.Count" />
    public int Count => _lines.Count;

    bool IList.IsReadOnly => true;

    bool IList.IsFixedSize => false;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => ((ICollection)_lines).SyncRoot;

    /// <inheritdoc />
    public OutputLine this[int index] => _lines[index];

    object? IList.this[int index]
    {
        get => _lines[index];
        set => throw new NotSupportedException();
    }

    /// <summary>Appends one line.</summary>
    public void Add(OutputLine line) => Append([line]);

    /// <summary>Appends a batch of lines with a single change notification.</summary>
    public void Append(IReadOnlyCollection<OutputLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Count == 0)
        {
            return;
        }

        if (lines.Count == 1 && _lines.Count < Capacity)
        {
            var line = lines.First();
            _lines.Add(line);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, line, _lines.Count - 1));
            RaiseCountsChanged();
            return;
        }

        _lines.AddRange(lines);
        if (_lines.Count > Capacity)
        {
            // Trim to 90% so dropping happens in chunks rather than on every new line.
            var remove = _lines.Count - (Capacity * 9 / 10);
            _lines.RemoveRange(0, remove);
            DroppedCount += remove;
        }

        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        RaiseCountsChanged();
    }

    /// <summary>Removes every line and resets <see cref="DroppedCount"/>.</summary>
    public void Clear()
    {
        _lines.Clear();
        DroppedCount = 0;
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        RaiseCountsChanged();
    }

    /// <summary>The kept lines as text (one per line), for copy and save.</summary>
    public string ToText() => string.Join(Environment.NewLine, _lines.Select(l => l.Text));

    /// <inheritdoc />
    public IEnumerator<OutputLine> GetEnumerator() => _lines.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    int IList.Add(object? value) => throw new NotSupportedException();

    void IList.Clear() => throw new NotSupportedException();

    bool IList.Contains(object? value) => value is OutputLine line && _lines.Contains(line);

    int IList.IndexOf(object? value) => value is OutputLine line ? _lines.IndexOf(line) : -1;

    void IList.Insert(int index, object? value) => throw new NotSupportedException();

    void IList.Remove(object? value) => throw new NotSupportedException();

    void IList.RemoveAt(int index) => throw new NotSupportedException();

    void ICollection.CopyTo(Array array, int index) => ((ICollection)_lines).CopyTo(array, index);

    private void RaiseCountsChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DroppedCount)));
    }
}

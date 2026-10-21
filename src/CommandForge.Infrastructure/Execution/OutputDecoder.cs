using System.Text;

namespace CommandForge.Infrastructure.Execution;

/// <summary>
/// Splits a child process's raw stdout/stderr bytes into text lines. Windows tools disagree on the
/// encoding of redirected output: most write the console's OEM code page (localized text on
/// Vietnamese/Japanese/Chinese Windows), newer tools write UTF-8, and sfc/wsl write UTF-16LE. So the
/// stream is sniffed once for UTF-16LE, and otherwise every line is decoded as strict UTF-8 with an
/// OEM-code-page fallback. Line breaks are \n, \r\n or a lone \r (progress updates).
/// </summary>
internal sealed class OutputDecoder
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly Encoding _fallback;
    private readonly List<byte> _pending = [];
    private bool? _utf16;
    private bool _bomChecked;

    public OutputDecoder(Encoding fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);
        _fallback = fallback;
    }

    /// <summary>Appends <paramref name="chunk"/> and adds every completed line to <paramref name="lines"/>.</summary>
    public void Push(ReadOnlySpan<byte> chunk, List<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        foreach (var b in chunk)
        {
            _pending.Add(b);
        }

        _utf16 ??= DetectUtf16(_pending);
        if (_utf16 is null)
        {
            return; // not enough bytes yet to tell UTF-16LE from a byte encoding
        }

        if (_utf16 == true && !_bomChecked && _pending.Count >= 2)
        {
            if (_pending[0] == 0xFF && _pending[1] == 0xFE)
            {
                _pending.RemoveRange(0, 2);
            }

            _bomChecked = true;
        }

        ExtractLines(lines, endOfStream: false);
    }

    /// <summary>Returns the trailing text after the last line break (call once at end of stream).</summary>
    public string? Flush()
    {
        _utf16 ??= false;
        var lines = new List<string>();
        ExtractLines(lines, endOfStream: true);
        return lines.Count == 0 ? null : string.Join(Environment.NewLine, lines);
    }

    private void ExtractLines(List<string> lines, bool endOfStream)
    {
        var unit = _utf16 == true ? 2 : 1;
        var start = 0;
        var i = 0;
        while (i + unit <= _pending.Count)
        {
            var code = CodeAt(i, unit);
            if (code == '\n')
            {
                lines.Add(Decode(start, i));
                i += unit;
                start = i;
            }
            else if (code == '\r')
            {
                var next = i + unit;
                if (next + unit > _pending.Count && !endOfStream)
                {
                    break; // can't tell yet whether a \n follows
                }

                lines.Add(Decode(start, i));
                var nextCode = next + unit <= _pending.Count ? CodeAt(next, unit) : -1;
                i = nextCode == '\n' ? next + unit : next;
                start = i;
            }
            else
            {
                i += unit;
            }
        }

        if (endOfStream && start < _pending.Count)
        {
            lines.Add(Decode(start, _pending.Count));
            start = _pending.Count;
        }

        _pending.RemoveRange(0, start);
    }

    /// <summary>The code unit at <paramref name="index"/>: one byte, or a little-endian UTF-16 unit.</summary>
    private int CodeAt(int index, int unit)
        => unit == 2 ? _pending[index] | (_pending[index + 1] << 8) : _pending[index];

    private string Decode(int start, int end)
    {
        if (end <= start)
        {
            return string.Empty;
        }

        var bytes = _pending.GetRange(start, end - start).ToArray();
        if (_utf16 == true)
        {
            return Encoding.Unicode.GetString(bytes);
        }

        try
        {
            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return _fallback.GetString(bytes);
        }
    }

    /// <summary>UTF-16LE when a BOM is present or most odd bytes of plain-ASCII-range text are zero.</summary>
    private static bool? DetectUtf16(List<byte> data)
    {
        if (data.Count >= 2 && data[0] == 0xFF && data[1] == 0xFE)
        {
            return true;
        }

        if (data.Count < 4)
        {
            return null; // decide once there is enough to look at
        }

        var sample = Math.Min(data.Count, 512) & ~1;
        var zeroOdd = 0;
        var zeroEven = 0;
        for (var i = 0; i < sample; i += 2)
        {
            if (data[i] == 0)
            {
                zeroEven++;
            }

            if (data[i + 1] == 0)
            {
                zeroOdd++;
            }
        }

        var pairs = sample / 2;
        return zeroOdd * 2 >= pairs && zeroEven * 4 < pairs;
    }
}

using System.Text;
using CommandForge.Infrastructure.Execution;

namespace CommandForge.Tests;

/// <summary>Tests line splitting and encoding detection for raw child-process output.</summary>
public sealed class OutputDecoderTests
{
    private static readonly Encoding Fallback = Encoding.Latin1;

    [Fact]
    public void Utf8_SplitsOnEveryKindOfLineBreak()
    {
        var lines = Decode(Encoding.UTF8.GetBytes("one\r\ntwo\nthree\rfour"), out var tail);

        Assert.Equal(["one", "two", "three"], lines);
        Assert.Equal("four", tail);
    }

    [Fact]
    public void CrLf_SplitAcrossChunks_IsOneLineBreak()
    {
        var decoder = new OutputDecoder(Fallback);
        var lines = new List<string>();

        decoder.Push(Encoding.UTF8.GetBytes("first line\r"), lines);
        decoder.Push(Encoding.UTF8.GetBytes("\nsecond\n"), lines);

        Assert.Equal(["first line", "second"], lines);
        Assert.Null(decoder.Flush());
    }

    [Fact]
    public void Utf8_MultiByteCharacterSplitAcrossChunks_IsKept()
    {
        var bytes = Encoding.UTF8.GetBytes("Xin chào thế giới\n");
        var decoder = new OutputDecoder(Fallback);
        var lines = new List<string>();

        // Cut inside the two-byte "à".
        var cut = Encoding.UTF8.GetByteCount("Xin ch") + 1;
        decoder.Push(bytes.AsSpan(0, cut), lines);
        decoder.Push(bytes.AsSpan(cut), lines);

        Assert.Equal(["Xin chào thế giới"], lines);
    }

    [Fact]
    public void InvalidUtf8_FallsBackToTheConsoleCodePage()
    {
        // "café" in a single-byte code page: 0xE9 alone is not valid UTF-8.
        var lines = Decode([(byte)'c', (byte)'a', (byte)'f', 0xE9, (byte)'\n'], out _);

        Assert.Equal(["café"], lines);
    }

    [Fact]
    public void Utf16LittleEndian_WithBom_IsDecoded()
    {
        var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("Default Version: 2\r\nXin chào\r\n")).ToArray();

        var lines = Decode(bytes, out var tail);

        Assert.Equal(["Default Version: 2", "Xin chào"], lines);
        Assert.Null(tail);
    }

    [Fact]
    public void Utf16LittleEndian_WithoutBom_IsDetected()
    {
        // wsl.exe writes UTF-16LE without a byte-order mark.
        var lines = Decode(Encoding.Unicode.GetBytes("Default Distribution: Ubuntu\r\n"), out _);

        Assert.Equal(["Default Distribution: Ubuntu"], lines);
    }

    [Fact]
    public void Utf16_CodeUnitSplitAcrossChunks_IsKept()
    {
        var bytes = Encoding.Unicode.GetBytes("Hello\r\n");
        var decoder = new OutputDecoder(Fallback);
        var lines = new List<string>();

        decoder.Push(bytes.AsSpan(0, 5), lines); // odd length: ends inside a UTF-16 code unit
        decoder.Push(bytes.AsSpan(5), lines);

        Assert.Equal(["Hello"], lines);
    }

    [Fact]
    public void ShortOutput_IsDeliveredByFlush()
    {
        var decoder = new OutputDecoder(Fallback);
        var lines = new List<string>();

        decoder.Push("ok"u8, lines);

        Assert.Empty(lines);
        Assert.Equal("ok", decoder.Flush());
    }

    [Fact]
    public void EmptyStream_FlushesNothing()
        => Assert.Null(new OutputDecoder(Fallback).Flush());

    [Fact]
    public void BlankLines_ArePreserved()
        => Assert.Equal(["a", string.Empty, "b"], Decode(Encoding.UTF8.GetBytes("a\r\n\r\nb\r\n"), out _));

    private static List<string> Decode(byte[] bytes, out string? tail)
    {
        var decoder = new OutputDecoder(Fallback);
        var lines = new List<string>();
        decoder.Push(bytes, lines);
        tail = decoder.Flush();
        return lines;
    }
}

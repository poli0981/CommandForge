using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace CommandForge.Infrastructure.Elevation;

/// <summary>
/// Length-prefixed JSON framing over a duplex stream (4-byte little-endian length + UTF-8 payload).
/// Writes are serialized; reads return <see langword="null"/> at end-of-stream.
/// </summary>
internal sealed class PipeMessageChannel : IDisposable
{
    private const int MaxMessageBytes = 16 * 1024 * 1024;

    private readonly Stream _stream;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public PipeMessageChannel(Stream stream) => _stream = stream;

    public async Task WriteAsync<T>(T message, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(message, typeInfo);
        var prefix = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(prefix, payload.Length);

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await _stream.WriteAsync(prefix, cancellationToken);
            await _stream.WriteAsync(payload, cancellationToken);
            await _stream.FlushAsync(cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<T?> ReadAsync<T>(JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken)
        where T : class
    {
        var prefix = new byte[4];
        if (!await ReadExactAsync(prefix, cancellationToken))
        {
            return null;
        }

        var length = BinaryPrimitives.ReadInt32LittleEndian(prefix);
        if (length is <= 0 or > MaxMessageBytes)
        {
            return null;
        }

        var payload = new byte[length];
        if (!await ReadExactAsync(payload, cancellationToken))
        {
            return null;
        }

        return JsonSerializer.Deserialize(payload, typeInfo);
    }

    private async Task<bool> ReadExactAsync(byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await _stream.ReadAsync(buffer.AsMemory(offset), cancellationToken);
            if (read == 0)
            {
                return false;
            }

            offset += read;
        }

        return true;
    }

    public void Dispose() => _writeLock.Dispose();
}

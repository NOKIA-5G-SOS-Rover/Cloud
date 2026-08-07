using System.Text;

namespace backend.Services;

/// <summary>
/// Pulls JPEG frames out of a multipart/x-mixed-replace response body, the format
/// served by mjpg-streamer, OpenCV/Flask camera servers and most IP cameras.
/// </summary>
internal sealed class MjpegStreamReader
{
    private static readonly byte[] HeaderTerminator = Encoding.ASCII.GetBytes("\r\n\r\n");

    private readonly Stream _source;
    private readonly byte[] _boundary;
    private readonly int _maxFrameBytes;

    private byte[] _buffer;
    private int _length;

    public MjpegStreamReader(Stream source, string boundary, int maxFrameBytes)
    {
        _source = source;
        _boundary = Encoding.ASCII.GetBytes("--" + boundary.Trim('"'));
        _maxFrameBytes = maxFrameBytes;
        _buffer = new byte[Math.Min(maxFrameBytes, 128 * 1024)];
    }

    public async Task<byte[]> ReadFrameAsync(CancellationToken cancellationToken)
    {
        var boundaryAt = await IndexOfAsync(_boundary, 0, cancellationToken);
        var headersAt = boundaryAt + _boundary.Length;

        var headersEnd = await IndexOfAsync(HeaderTerminator, headersAt, cancellationToken);
        var bodyStart = headersEnd + HeaderTerminator.Length;

        var headers = Encoding.ASCII.GetString(_buffer, headersAt, headersEnd - headersAt);
        var declaredLength = ParseContentLength(headers);

        int bodyEnd;

        if (declaredLength > 0)
        {
            if (declaredLength > _maxFrameBytes)
            {
                throw new InvalidDataException(
                    $"Upstream frame of {declaredLength} bytes exceeds the {_maxFrameBytes} byte limit."
                );
            }

            await EnsureBufferedAsync(bodyStart + declaredLength, cancellationToken);
            bodyEnd = bodyStart + declaredLength;
        }
        else
        {
            // No Content-Length: the frame runs up to the next boundary marker.
            bodyEnd = await IndexOfAsync(_boundary, bodyStart, cancellationToken);

            while (bodyEnd > bodyStart && (_buffer[bodyEnd - 1] == (byte)'\n' || _buffer[bodyEnd - 1] == (byte)'\r'))
            {
                bodyEnd--;
            }
        }

        var frame = _buffer.AsSpan(bodyStart, bodyEnd - bodyStart).ToArray();
        Consume(bodyEnd);

        return frame;
    }

    private async Task<int> IndexOfAsync(byte[] pattern, int from, CancellationToken cancellationToken)
    {
        var searchFrom = from;

        while (true)
        {
            var index = IndexOf(_buffer.AsSpan(0, _length), pattern, searchFrom);

            if (index >= 0)
            {
                return index;
            }

            // A match may straddle the boundary between what we have and what comes
            // next, so back up by just under the pattern length before re-scanning.
            searchFrom = Math.Max(from, _length - pattern.Length + 1);

            await ReadMoreAsync(cancellationToken);
        }
    }

    private async Task EnsureBufferedAsync(int required, CancellationToken cancellationToken)
    {
        while (_length < required)
        {
            await ReadMoreAsync(cancellationToken);
        }
    }

    private async Task ReadMoreAsync(CancellationToken cancellationToken)
    {
        if (_length == _buffer.Length)
        {
            var capacity = _buffer.Length * 2;

            if (capacity > _maxFrameBytes + (64 * 1024))
            {
                throw new InvalidDataException(
                    "Upstream MJPEG stream produced no complete frame within the size limit."
                );
            }

            Array.Resize(ref _buffer, capacity);
        }

        var read = await _source.ReadAsync(_buffer.AsMemory(_length), cancellationToken);

        if (read == 0)
        {
            throw new EndOfStreamException("Upstream MJPEG stream closed.");
        }

        _length += read;
    }

    private void Consume(int count)
    {
        Buffer.BlockCopy(_buffer, count, _buffer, 0, _length - count);
        _length -= count;
    }

    private static int IndexOf(ReadOnlySpan<byte> haystack, ReadOnlySpan<byte> needle, int start)
    {
        if (start >= haystack.Length)
        {
            return -1;
        }

        var found = haystack[start..].IndexOf(needle);

        return found < 0 ? -1 : found + start;
    }

    private static int ParseContentLength(string headers)
    {
        foreach (var line in headers.Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = line.IndexOf(':');

            if (separator < 0)
            {
                continue;
            }

            if (!line.AsSpan(0, separator).Trim().Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (int.TryParse(line.AsSpan(separator + 1).Trim(), out var length))
            {
                return length;
            }
        }

        return -1;
    }
}

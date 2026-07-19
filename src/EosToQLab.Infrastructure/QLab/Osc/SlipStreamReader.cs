using EosToQLab.Core.Exceptions;

namespace EosToQLab.Infrastructure.QLab.Osc;

/// <summary>
/// Stateful buffered reader for double-END SLIP frames.
/// It preserves bytes already read beyond the current frame, avoiding one
/// asynchronous stream read per byte while keeping frame boundaries intact.
/// </summary>
internal sealed class SlipStreamReader
{
    private readonly Stream _stream;
    private readonly byte[] _buffer;
    private int _offset;
    private int _length;

    public SlipStreamReader(Stream stream, int bufferSize = 8192)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentOutOfRangeException.ThrowIfLessThan(bufferSize, 1);

        _stream = stream;
        _buffer = new byte[bufferSize];
    }

    public async Task<byte[]> ReadFrameAsync(CancellationToken cancellationToken)
    {
        var payload = new List<byte>(256);
        var escaped = false;
        var started = false;

        while (true)
        {
            if (_offset >= _length)
            {
                _length = await _stream.ReadAsync(_buffer.AsMemory(), cancellationToken);
                _offset = 0;
                if (_length == 0) throw new QLabConnectionClosedException();
            }

            while (_offset < _length)
            {
                var value = _buffer[_offset++];
                if (value == SlipCodec.End)
                {
                    if (!started || payload.Count == 0)
                    {
                        started = true;
                        escaped = false;
                        payload.Clear();
                        continue;
                    }

                    return payload.ToArray();
                }

                started = true;
                if (escaped)
                {
                    payload.Add(value switch
                    {
                        SlipCodec.EscapedEnd => SlipCodec.End,
                        SlipCodec.EscapedEscape => SlipCodec.Escape,
                        _ => throw new QLabSlipFrameException(value)
                    });
                    escaped = false;
                }
                else if (value == SlipCodec.Escape)
                {
                    escaped = true;
                }
                else
                {
                    payload.Add(value);
                }
            }
        }
    }
}

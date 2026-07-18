using EosToQLab.Core.Exceptions;

namespace EosToQLab.Infrastructure.QLab.Osc;

internal static class SlipCodec
{
    private const byte End = 0xC0;
    private const byte Escape = 0xDB;
    private const byte EscapedEnd = 0xDC;
    private const byte EscapedEscape = 0xDD;

    public static byte[] Frame(ReadOnlySpan<byte> payload)
    {
        using var stream = new MemoryStream(payload.Length + 4);
        stream.WriteByte(End);
        foreach (var value in payload)
        {
            switch (value)
            {
                case End:
                    stream.WriteByte(Escape);
                    stream.WriteByte(EscapedEnd);
                    break;
                case Escape:
                    stream.WriteByte(Escape);
                    stream.WriteByte(EscapedEscape);
                    break;
                default:
                    stream.WriteByte(value);
                    break;
            }
        }
        stream.WriteByte(End);
        stream.WriteByte(End);
        return stream.ToArray();
    }

    public static async Task<byte[]> ReadFrameAsync(Stream stream, CancellationToken cancellationToken)
    {
        var payload = new List<byte>();
        var escaped = false;
        var started = false;
        var buffer = new byte[1];

        while (true)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                throw new QLabConnectionClosedException();
            }

            var value = buffer[0];
            if (value == End)
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
                    EscapedEnd => End,
                    EscapedEscape => Escape,
                    _ => throw new QLabSlipFrameException(value)
                });
                escaped = false;
            }
            else if (value == Escape)
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

namespace EosToQLab.Infrastructure.QLab.Osc;

internal static class SlipCodec
{
    internal const byte End = 0xC0;
    internal const byte Escape = 0xDB;
    internal const byte EscapedEnd = 0xDC;
    internal const byte EscapedEscape = 0xDD;

    public static byte[] Frame(ReadOnlySpan<byte> payload)
    {
        using var stream = new MemoryStream(payload.Length + 4);
        stream.WriteByte(End);
        foreach (var value in payload)
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

        stream.WriteByte(End);
        stream.WriteByte(End);
        return stream.ToArray();
    }

}

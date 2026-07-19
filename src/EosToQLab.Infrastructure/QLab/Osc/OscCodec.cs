using System.Buffers.Binary;
using System.Text;
using EosToQLab.Core.Exceptions;

namespace EosToQLab.Infrastructure.QLab.Osc;

internal static class OscCodec
{
    public static byte[] Encode(OscMessage message)
    {
        using var stream = new MemoryStream();
        WritePaddedString(stream, message.Address);
        var tags = new StringBuilder(",");
        foreach (var argument in message.Arguments) AppendTypeTag(tags, argument);
        WritePaddedString(stream, tags.ToString());
        foreach (var argument in message.Arguments) WriteArgument(stream, argument);
        return stream.ToArray();
    }

    public static OscMessage Decode(ReadOnlySpan<byte> data)
    {
        var offset = 0;
        var address = ReadPaddedString(data, ref offset);
        var typeTags = ReadPaddedString(data, ref offset);
        if (!typeTags.StartsWith(','))
            throw new QLabOscPacketException("The OSC type tag string does not start with a comma.");

        var tagIndex = 1;
        var arguments = ReadArguments(data, ref offset, typeTags, ref tagIndex, false);
        return new OscMessage(address, arguments);
    }

    private static List<object?> ReadArguments(
        ReadOnlySpan<byte> data,
        ref int offset,
        string typeTags,
        ref int tagIndex,
        bool stopAtArrayEnd)
    {
        var values = new List<object?>();
        while (tagIndex < typeTags.Length)
        {
            var tag = typeTags[tagIndex++];
            if (tag == ']')
            {
                if (!stopAtArrayEnd)
                    throw new QLabOscPacketException(
                        "The OSC type tag string contains an unexpected array terminator.");
                return values;
            }

            switch (tag)
            {
                case '[':
                    values.Add(ReadArguments(data, ref offset, typeTags, ref tagIndex, true));
                    break;
                case 's':
                    values.Add(ReadPaddedString(data, ref offset));
                    break;
                case 'i':
                    EnsureAvailable(data, offset, 4);
                    values.Add(BinaryPrimitives.ReadInt32BigEndian(data[offset..]));
                    offset += 4;
                    break;
                case 'f':
                    EnsureAvailable(data, offset, 4);
                    values.Add(BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32BigEndian(data[offset..])));
                    offset += 4;
                    break;
                case 'd':
                    EnsureAvailable(data, offset, 8);
                    values.Add(BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(data[offset..])));
                    offset += 8;
                    break;
                case 'T':
                    values.Add(true);
                    break;
                case 'F':
                    values.Add(false);
                    break;
                case 'N':
                    values.Add(null);
                    break;
                case 'b':
                    EnsureAvailable(data, offset, 4);
                    var length = BinaryPrimitives.ReadInt32BigEndian(data[offset..]);
                    offset += 4;
                    if (length < 0) throw new QLabOscPacketException("The OSC blob length is negative.");
                    EnsureAvailable(data, offset, length);
                    values.Add(data.Slice(offset, length).ToArray());
                    offset = Align4(offset + length);
                    break;
                default:
                    throw new QLabOscPacketException($"OSC type tag '{tag}' is not supported.");
            }
        }

        if (stopAtArrayEnd) throw new QLabOscPacketException("The OSC type tag string contains an unterminated array.");

        return values;
    }

    private static void AppendTypeTag(StringBuilder tags, object? argument)
    {
        switch (argument)
        {
            case null:
                tags.Append('N');
                break;
            case string:
                tags.Append('s');
                break;
            case int:
                tags.Append('i');
                break;
            case float:
                tags.Append('f');
                break;
            case double or decimal:
                tags.Append('d');
                break;
            case bool boolean:
                tags.Append(boolean ? 'T' : 'F');
                break;
            case byte[]:
                tags.Append('b');
                break;
            case IEnumerable<object?> collection:
                tags.Append('[');
                foreach (var item in collection) AppendTypeTag(tags, item);
                tags.Append(']');
                break;
            default:
                throw new QLabOscPacketException(
                    $"OSC argument type '{argument.GetType().FullName}' is not supported.");
        }
    }

    private static void WriteArgument(Stream stream, object? argument)
    {
        switch (argument)
        {
            case null:
            case bool:
                return;
            case string text:
                WritePaddedString(stream, text);
                return;
            case int integer:
                Span<byte> intBuffer = stackalloc byte[4];
                BinaryPrimitives.WriteInt32BigEndian(intBuffer, integer);
                stream.Write(intBuffer);
                return;
            case float single:
                Span<byte> floatBuffer = stackalloc byte[4];
                BinaryPrimitives.WriteInt32BigEndian(floatBuffer, BitConverter.SingleToInt32Bits(single));
                stream.Write(floatBuffer);
                return;
            case double doubleValue:
                Span<byte> doubleBuffer = stackalloc byte[8];
                BinaryPrimitives.WriteInt64BigEndian(doubleBuffer, BitConverter.DoubleToInt64Bits(doubleValue));
                stream.Write(doubleBuffer);
                return;
            case decimal decimalValue:
                WriteArgument(stream, (double)decimalValue);
                return;
            case byte[] blob:
                Span<byte> lengthBuffer = stackalloc byte[4];
                BinaryPrimitives.WriteInt32BigEndian(lengthBuffer, blob.Length);
                stream.Write(lengthBuffer);
                stream.Write(blob);
                WritePadding(stream, blob.Length);
                return;
            case IEnumerable<object?> collection:
                foreach (var item in collection) WriteArgument(stream, item);
                return;
            default:
                throw new QLabOscPacketException(
                    $"OSC argument type '{argument.GetType().FullName}' is not supported.");
        }
    }

    private static void WritePaddedString(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        stream.Write(bytes);
        stream.WriteByte(0);
        WritePadding(stream, bytes.Length + 1);
    }

    private static string ReadPaddedString(ReadOnlySpan<byte> data, ref int offset)
    {
        if (offset >= data.Length)
            throw new QLabOscPacketException("The OSC packet ended before a string could be read.");

        var relativeEnd = data[offset..].IndexOf((byte)0);
        if (relativeEnd < 0) throw new QLabOscPacketException("The OSC packet contains an unterminated string.");

        var value = Encoding.UTF8.GetString(data.Slice(offset, relativeEnd));
        offset = Align4(offset + relativeEnd + 1);
        if (offset > data.Length) throw new QLabOscPacketException("The OSC string padding extends beyond the packet.");
        return value;
    }

    private static void WritePadding(Stream stream, int bytesWritten)
    {
        var padding = (4 - bytesWritten % 4) % 4;
        for (var index = 0; index < padding; index++) stream.WriteByte(0);
    }

    private static int Align4(int value)
    {
        return (value + 3) & ~3;
    }

    private static void EnsureAvailable(ReadOnlySpan<byte> data, int offset, int count)
    {
        if (count < 0 || offset < 0 || offset + count > data.Length)
            throw new QLabOscPacketException("The OSC packet ended unexpectedly.");
    }
}
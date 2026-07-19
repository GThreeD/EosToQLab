using System.Text;

namespace EosToQLab.Tests.Infrastructure.QLab.Osc;

public sealed class OscCodecTests
{
    [Fact]
    public void Round_trips_every_supported_argument_type_and_arrays()
    {
        var message = new OscMessage("/test", "text", 42, 1.25f, 2.5d, 3.75m, true, false, null, new byte[] { 1, 2, 3 },
            new object?[] { "nested", 9, null });
        var decoded = OscCodec.Decode(OscCodec.Encode(message));

        Assert.Equal("/test", decoded.Address);
        Assert.Equal("text", decoded.Arguments[0]);
        Assert.Equal(42, decoded.Arguments[1]);
        Assert.Equal(1.25f, decoded.Arguments[2]);
        Assert.Equal(2.5d, decoded.Arguments[3]);
        Assert.Equal(3.75d, decoded.Arguments[4]);
        Assert.Equal(true, decoded.Arguments[5]);
        Assert.Equal(false, decoded.Arguments[6]);
        Assert.Null(decoded.Arguments[7]);
        Assert.Equal(new byte[] { 1, 2, 3 }, Assert.IsType<byte[]>(decoded.Arguments[8]));
        var nested = Assert.IsAssignableFrom<IReadOnlyList<object?>>(decoded.Arguments[9]);
        Assert.Equal("nested", nested[0]);
        Assert.Equal(9, nested[1]);
        Assert.Null(nested[2]);
    }

    [Fact]
    public void Rejects_unsupported_encode_types()
    {
        Assert.Throws<QLabOscPacketException>(() => OscCodec.Encode(new OscMessage("/test", DateTime.UtcNow)));
    }

    [Fact]
    public void Rejects_invalid_type_tag_and_array_shapes()
    {
        Assert.Throws<QLabOscPacketException>(() => OscCodec.Decode(Packet("/test", "i", [])));
        Assert.Throws<QLabOscPacketException>(() => OscCodec.Decode(Packet("/test", ",]", [])));
        Assert.Throws<QLabOscPacketException>(() => OscCodec.Decode(Packet("/test", ",[", [])));
        Assert.Throws<QLabOscPacketException>(() => OscCodec.Decode(Packet("/test", ",x", [])));
    }

    [Fact]
    public void Rejects_truncated_strings_blobs_and_numeric_values()
    {
        Assert.Throws<QLabOscPacketException>(() => OscCodec.Decode([]));
        Assert.Throws<QLabOscPacketException>(() => OscCodec.Decode(Encoding.UTF8.GetBytes("unterminated")));
        Assert.Throws<QLabOscPacketException>(() => OscCodec.Decode(Packet("/test", ",i", [0, 0])));
        Assert.Throws<QLabOscPacketException>(() => OscCodec.Decode(Packet("/test", ",b", [255, 255, 255, 255])));
        Assert.Throws<QLabOscPacketException>(() => OscCodec.Decode(Packet("/test", ",b", [0, 0, 0, 5, 1])));
    }

    private static byte[] Packet(string address, string tags, byte[] payload)
    {
        using var stream = new MemoryStream();
        WriteString(stream, address);
        WriteString(stream, tags);
        stream.Write(payload);
        return stream.ToArray();
    }

    private static void WriteString(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        stream.Write(bytes);
        stream.WriteByte(0);
        while (stream.Length % 4 != 0) stream.WriteByte(0);
    }
}
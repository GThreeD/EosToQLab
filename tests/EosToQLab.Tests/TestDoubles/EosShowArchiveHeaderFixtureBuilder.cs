using System.Text;

namespace EosToQLab.Tests.TestDoubles;

internal static class EosShowArchiveHeaderFixtureBuilder
{
    public static (byte[] Data, int CueNumberEnd, int RecordEnd, int? NotesOffset) Build(
        byte[] followValue,
        string? notes = null,
        byte[]? continuation = null)
    {
        var bytes = new List<byte>();
        for (var index = 0; index < 5; index++) bytes.Add(0x00);
        bytes.Add(0x02);
        for (var index = 0; index < 13; index++) bytes.AddRange(index == 5 ? followValue : [0x00]);
        int? notesOffset = null;
        if (notes is not null)
        {
            notesOffset = bytes.Count;
            bytes.AddRange(Text(notes));
        }
        else
        {
            bytes.Add(0x04);
        }

        if (continuation is not null) bytes.AddRange(continuation);
        return (bytes.ToArray(), 0, bytes.Count, notesOffset);
    }

    public static byte[] Text(string value)
    {
        var text = Encoding.Unicode.GetBytes(value);
        var result = new List<byte> { 0x03, (byte)value.Length, (byte)(value.Length >> 8) };
        result.AddRange(text);
        return result.ToArray();
    }

    public static byte[] Unsigned(int value)
    {
        if (value <= byte.MaxValue) return [0x08, (byte)value];
        if (value <= ushort.MaxValue) return [0x09, (byte)value, (byte)(value >> 8)];
        return [0x0A, (byte)value, (byte)(value >> 8), (byte)(value >> 16)];
    }

    public static byte[] Boolean(bool value)
    {
        return [0x01, value ? (byte)1 : (byte)0];
    }

    public static byte[] Continuation(
        bool isHang,
        int milliseconds,
        bool firstLegacyMode = false,
        bool secondLegacyMode = false)
    {
        return
        [
            0x02,
            .. Boolean(firstLegacyMode),
            .. Unsigned(1),
            .. Boolean(secondLegacyMode),
            .. Unsigned(1),
            .. Unsigned(milliseconds),
            isHang ? (byte)0x00 : (byte)0x04,
            .. Unsigned(1)
        ];
    }
}
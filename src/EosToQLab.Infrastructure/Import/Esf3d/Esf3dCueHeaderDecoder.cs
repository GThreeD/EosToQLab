using System.Globalization;
using System.Text;

namespace EosToQLab.Infrastructure.Import.Esf3d;

internal static class Esf3dCueHeaderDecoder
{
    private const byte TextTag = 0x03;
    private const int FollowFieldIndex = 5;
    private const int MetadataFieldCount = 13;

    public static Esf3dCueHeaderDecodeResult Decode(byte[] data, int cueNumberEnd, int recordEnd)
    {
        var offset = cueNumberEnd;

        // label, parent reference, two GUIDs, target reference, metadata object
        if (!SkipValue(data, ref offset, recordEnd)
            || !SkipValue(data, ref offset, recordEnd)
            || !SkipValue(data, ref offset, recordEnd)
            || !SkipValue(data, ref offset, recordEnd)
            || !SkipValue(data, ref offset, recordEnd)
            || offset >= recordEnd
            || data[offset++] != 0x02)
        {
            return Esf3dCueHeaderDecodeResult.Unparsed;
        }

        string? follow = null;
        int? followTextOffset = null;

        for (var index = 0; index < MetadataFieldCount; index++)
        {
            if (index != FollowFieldIndex)
            {
                if (!SkipValue(data, ref offset, recordEnd))
                {
                    return Esf3dCueHeaderDecodeResult.UnparsedWithFollow(follow, followTextOffset);
                }

                continue;
            }

            followTextOffset = data[offset] == TextTag ? offset : null;
            if (!TryReadFollow(data, offset, recordEnd, out follow, out var followEnd))
            {
                var unsupported = offset < recordEnd && data[offset] != 0x00 && data[offset] != 0x04;
                return new Esf3dCueHeaderDecodeResult(
                    Parsed: false,
                    Follow: null,
                    FollowDecodeFailed: unsupported,
                    CueNotesOffset: null,
                    FollowTextOffset: followTextOffset);
            }

            offset = followEnd;
        }

        int? cueNotesOffset = IsTextValue(data, offset, recordEnd) ? offset : null;
        return new Esf3dCueHeaderDecodeResult(
            Parsed: true,
            Follow: follow,
            FollowDecodeFailed: false,
            CueNotesOffset: cueNotesOffset,
            FollowTextOffset: followTextOffset);
    }

    private static bool TryReadFollow(
        byte[] data,
        int offset,
        int end,
        out string? value,
        out int valueEnd)
    {
        value = null;
        valueEnd = offset;
        if (offset >= end)
        {
            return false;
        }

        if (data[offset] is 0x00 or 0x04)
        {
            valueEnd = offset + 1;
            return true;
        }

        if (TryReadText(data, offset, end, out var text, out valueEnd))
        {
            value = NormalizeFollowText(text);
            return value is not null;
        }

        if (TryReadUnsigned(data, offset, end, out var milliseconds, out valueEnd))
        {
            value = FormatFollowOrHang(isHang: false, milliseconds);
            return true;
        }

        if (data[offset] == 0x01 && offset + 1 < end)
        {
            value = FormatFollowOrHang(data[offset + 1] != 0, milliseconds: null);
            valueEnd = offset + 2;
            return true;
        }

        return data[offset] == 0x02
            && TryReadFollowObject(data, offset + 1, Math.Min(end, offset + 32), out value, out valueEnd);
    }

    private static bool TryReadFollowObject(
        byte[] data,
        int offset,
        int end,
        out string? value,
        out int valueEnd)
    {
        value = null;
        valueEnd = offset;
        bool? isHang = null;
        var numbers = new List<int>(2);

        while (offset < end && numbers.Count < 2)
        {
            if (data[offset] is 0x00 or 0x04)
            {
                valueEnd = ++offset;
                if (isHang.HasValue || numbers.Count > 0)
                {
                    break;
                }

                continue;
            }

            if (data[offset] == 0x02)
            {
                offset++;
                continue;
            }

            if (data[offset] == 0x01 && offset + 1 < end)
            {
                isHang = data[offset + 1] != 0;
                offset += 2;
                valueEnd = offset;
                continue;
            }

            if (TryReadText(data, offset, end, out var text, out valueEnd))
            {
                value = NormalizeFollowText(text);
                return value is not null;
            }

            if (!TryReadUnsigned(data, offset, end, out var number, out var numberEnd))
            {
                return false;
            }

            numbers.Add(number);
            offset = numberEnd;
            valueEnd = offset;
        }

        if (!isHang.HasValue && numbers.Count >= 2 && numbers[0] is 0 or 1)
        {
            isHang = numbers[0] == 1;
            numbers.RemoveAt(0);
        }

        if (!isHang.HasValue && numbers.Count == 0)
        {
            return false;
        }

        value = FormatFollowOrHang(isHang ?? false, numbers.Count == 0 ? null : numbers[0]);
        return true;
    }

    private static bool SkipValue(byte[] data, ref int offset, int end)
    {
        if (offset >= end)
        {
            return false;
        }

        var length = data[offset] switch
        {
            0x00 or 0x02 or 0x04 => 1,
            0x01 or 0x08 => 2,
            0x09 => 3,
            0x0A => 4,
            0x06 => 9,
            TextTag when offset + 2 < end =>
                3 + (data[offset + 1] | (data[offset + 2] << 8)) * 2,
            _ => 0
        };

        if (length == 0 || offset + length > end)
        {
            return false;
        }

        offset += length;
        return true;
    }

    private static bool IsTextValue(byte[] data, int offset, int end) =>
        TryReadText(data, offset, end, out _, out _);

    private static bool TryReadText(
        byte[] data,
        int offset,
        int end,
        out string text,
        out int valueEnd)
    {
        text = string.Empty;
        valueEnd = offset;
        if (offset + 2 >= end || data[offset] != TextTag)
        {
            return false;
        }

        var characterCount = data[offset + 1] | (data[offset + 2] << 8);
        valueEnd = offset + 3 + characterCount * 2;
        if (valueEnd > end)
        {
            return false;
        }

        try
        {
            text = new UnicodeEncoding(false, false, true)
                .GetString(data, offset + 3, characterCount * 2);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private static bool TryReadUnsigned(
        byte[] data,
        int offset,
        int end,
        out int value,
        out int valueEnd)
    {
        value = 0;
        valueEnd = offset;
        if (offset >= end)
        {
            return false;
        }

        var width = data[offset] switch
        {
            0x08 => 1,
            0x09 => 2,
            0x0A => 3,
            _ => 0
        };
        if (width == 0 || offset + 1 + width > end)
        {
            return false;
        }

        for (var index = 0; index < width; index++)
        {
            value |= data[offset + 1 + index] << (8 * index);
        }

        valueEnd = offset + 1 + width;
        return true;
    }

    private static string? NormalizeFollowText(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed.StartsWith("follow", StringComparison.OrdinalIgnoreCase))
        {
            return $"F{trimmed[6..].TrimStart(' ', ':')}";
        }
        if (trimmed.StartsWith("hang", StringComparison.OrdinalIgnoreCase))
        {
            return $"H{trimmed[4..].TrimStart(' ', ':')}";
        }
        if (trimmed[0] is 'F' or 'f' or 'H' or 'h')
        {
            return char.ToUpperInvariant(trimmed[0]) + trimmed[1..];
        }

        return null;
    }

    private static string FormatFollowOrHang(bool isHang, int? milliseconds)
    {
        var prefix = isHang ? "H" : "F";
        if (milliseconds is null)
        {
            return prefix;
        }

        var seconds = (decimal)milliseconds.Value / 1_000m;
        return prefix + seconds.ToString("0.###", CultureInfo.InvariantCulture);
    }
}

internal sealed record Esf3dCueHeaderDecodeResult(
    bool Parsed,
    string? Follow,
    bool FollowDecodeFailed,
    int? CueNotesOffset,
    int? FollowTextOffset)
{
    public static Esf3dCueHeaderDecodeResult Unparsed { get; } =
        new(false, null, false, null, null);

    public static Esf3dCueHeaderDecodeResult UnparsedWithFollow(
        string? follow,
        int? followTextOffset) =>
        new(false, follow, false, null, followTextOffset);
}

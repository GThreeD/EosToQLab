using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Infrastructure.Import.Esf3d;

public sealed class Esf3dCueHeaderDecoderTests
{
    [Theory]
    [InlineData("F1", "F1")]
    [InlineData("follow: 1.5", "F1.5")]
    [InlineData("hang 2", "H2")]
    [InlineData(" h3 ", "H3")]
    public void Decodes_text_follow_variants(string input, string expected)
    {
        var fixture = Esf3dHeaderFixtureBuilder.Build(Esf3dHeaderFixtureBuilder.Text(input), "note");
        var result = Esf3dCueHeaderDecoder.Decode(fixture.Data, fixture.CueNumberEnd, fixture.RecordEnd);
        Assert.True(result.Parsed);
        Assert.Equal(expected, result.Follow);
        Assert.Equal(fixture.NotesOffset, result.CueNotesOffset);
        Assert.NotNull(result.FollowTextOffset);
        Assert.False(result.FollowDecodeFailed);
    }

    [Fact]
    public void Decodes_unsigned_boolean_and_compact_object_variants()
    {
        Assert.Equal("F1.5", Decode(Esf3dHeaderFixtureBuilder.Unsigned(1500)).Follow);
        Assert.Equal("H", Decode(Esf3dHeaderFixtureBuilder.Boolean(true)).Follow);
        Assert.Equal("H1.5",
            Decode([
                0x02, .. Esf3dHeaderFixtureBuilder.Boolean(true), .. Esf3dHeaderFixtureBuilder.Unsigned(1500), 0x04
            ]).Follow);
        Assert.Equal("F1.5",
            Decode([0x02, .. Esf3dHeaderFixtureBuilder.Unsigned(0), .. Esf3dHeaderFixtureBuilder.Unsigned(1500), 0x04])
                .Follow);
    }

    [Fact]
    public void Decodes_current_continuation_object_when_legacy_field_is_null()
    {
        var fixture = Esf3dHeaderFixtureBuilder.Build([0x00],
            continuation: Esf3dHeaderFixtureBuilder.Continuation(false, true, 1250));
        var result = Esf3dCueHeaderDecoder.Decode(fixture.Data, 0, fixture.RecordEnd);
        Assert.Equal("H1.25", result.Follow);
    }

    [Fact]
    public void Null_follow_is_valid_and_unknown_encoding_is_reported()
    {
        var empty = Decode([0x04]);
        Assert.True(empty.Parsed);
        Assert.Null(empty.Follow);
        Assert.False(empty.FollowDecodeFailed);

        var unsupported = Decode([0x07]);
        Assert.False(unsupported.Parsed);
        Assert.True(unsupported.FollowDecodeFailed);
    }

    [Fact]
    public void Invalid_header_is_unparsed()
    {
        var result = Esf3dCueHeaderDecoder.Decode([0x00], 0, 1);
        Assert.Equal(Esf3dCueHeaderDecodeResult.Unparsed, result);
        var withFollow = Esf3dCueHeaderDecodeResult.UnparsedWithFollow("F1", 3);
        Assert.False(withFollow.Parsed);
        Assert.Equal("F1", withFollow.Follow);
        Assert.Equal(3, withFollow.FollowTextOffset);
    }

    private static Esf3dCueHeaderDecodeResult Decode(byte[] follow)
    {
        var fixture = Esf3dHeaderFixtureBuilder.Build(follow);
        return Esf3dCueHeaderDecoder.Decode(fixture.Data, 0, fixture.RecordEnd);
    }
}
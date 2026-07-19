namespace EosToQLab.Tests.Infrastructure.Import.Esf3d;

public sealed class Esf3dCueHeaderDecodeResultTests
{
    [Fact]
    public void Stores_decoded_header_values()
    {
        var result = new Esf3dCueHeaderDecodeResult(
            true,
            "F1",
            false,
            12,
            24);

        Assert.True(result.Parsed);
        Assert.Equal("F1", result.Follow);
        Assert.False(result.FollowDecodeFailed);
        Assert.Equal(12, result.CueNotesOffset);
        Assert.Equal(24, result.FollowTextOffset);
        Assert.False(Esf3dCueHeaderDecodeResult.Unparsed.Parsed);
    }
}
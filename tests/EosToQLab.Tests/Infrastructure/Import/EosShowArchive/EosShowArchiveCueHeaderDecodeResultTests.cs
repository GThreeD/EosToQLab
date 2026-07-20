namespace EosToQLab.Tests.Infrastructure.Import.EosShowArchive;

public sealed class EosShowArchiveCueHeaderDecodeResultTests
{
    [Fact]
    public void Stores_decoded_header_values()
    {
        var result = new EosShowArchiveCueHeaderDecodeResult(
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
        Assert.False(EosShowArchiveCueHeaderDecodeResult.Unparsed.Parsed);
    }
}
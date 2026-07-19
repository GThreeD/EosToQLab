namespace EosToQLab.Tests.Infrastructure.QLab;

public sealed class QLabCueTypeTests
{
    [Fact]
    public void Defines_supported_cue_types()
    {
        Assert.Equal([QLabCueType.CueList, QLabCueType.Memo, QLabCueType.Network], Enum.GetValues<QLabCueType>());
    }
}
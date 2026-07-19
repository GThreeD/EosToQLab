namespace EosToQLab.Tests.Infrastructure.QLab;

public sealed class QLabCuePropertyTests
{
    [Fact]
    public void Defines_supported_cue_properties()
    {
        Assert.Equal(
            [
                QLabCueProperty.Name,
                QLabCueProperty.Number,
                QLabCueProperty.Notes,
                QLabCueProperty.Armed,
                QLabCueProperty.SkipIfDisarmed,
                QLabCueProperty.NetworkPatchId
            ],
            Enum.GetValues<QLabCueProperty>());
    }
}
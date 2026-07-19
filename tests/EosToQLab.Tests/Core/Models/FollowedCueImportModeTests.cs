namespace EosToQLab.Tests.Core.Models;

public sealed class FollowedCueImportModeTests
{
    [Fact]
    public void Defines_expected_values()
    {
        Assert.NotEmpty(Enum.GetValues<FollowedCueImportMode>());
    }
}
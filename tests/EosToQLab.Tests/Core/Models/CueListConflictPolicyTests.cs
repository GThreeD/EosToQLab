namespace EosToQLab.Tests.Core.Models;

public sealed class CueListConflictPolicyTests
{
    [Fact]
    public void Defines_expected_values()
    {
        Assert.NotEmpty(Enum.GetValues<CueListConflictPolicy>());
    }
}
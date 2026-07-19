namespace EosToQLab.Tests.Core.Models;

public sealed class EosSourceKindTests
{
    [Fact]
    public void Defines_expected_values()
    {
        Assert.NotEmpty(Enum.GetValues<EosSourceKind>());
    }
}
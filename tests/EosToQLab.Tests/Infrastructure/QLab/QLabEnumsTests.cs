namespace EosToQLab.Tests.Infrastructure.QLab;

public sealed class QLabEnumsTests
{
    [Fact]
    public void Protocol_enums_define_expected_values()
    {
        Assert.Equal(3, Enum.GetValues<QLabCueType>().Length);
        Assert.Equal(6, Enum.GetValues<QLabCueProperty>().Length);
        Assert.Single(Enum.GetValues<QLabWorkspaceProperty>());
    }
}
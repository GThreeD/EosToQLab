namespace EosToQLab.Tests.Core.QLab;

public sealed class IQLabServiceTests
{
    [Fact]
    public void Defines_application_qlab_contract()
    {
        Assert.True(typeof(IQLabService).IsInterface);
    }
}
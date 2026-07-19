namespace EosToQLab.Tests.Core.Security;

public sealed class IQLabPasscodeStoreTests
{
    [Fact]
    public void Session_store_implements_contract()
    {
        Assert.IsAssignableFrom<IQLabPasscodeStore>(new SessionQLabPasscodeStore());
    }
}
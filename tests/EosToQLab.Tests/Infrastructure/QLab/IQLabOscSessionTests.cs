using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Infrastructure.QLab;

public sealed class IQLabOscSessionTests
{
    [Fact]
    public void Fake_implements_contract()
    {
        Assert.IsAssignableFrom<IQLabOscSession>(new FakeQLabOscSession());
    }
}
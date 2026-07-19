using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Infrastructure.QLab;

public sealed class IQLabOscServiceTests
{
    [Fact]
    public void Fake_implements_contract()
    {
        Assert.IsType<IQLabOscService>(new FakeQLabOscService(new FakeQLabOscSession()), false);
    }
}
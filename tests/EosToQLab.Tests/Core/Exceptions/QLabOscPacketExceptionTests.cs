using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabOscPacketExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new QLabOscPacketException("invalid", inner);

        ExceptionAssertions.HasDetails(exception, "QLAB_OSC_PACKET_INVALID", "invalid");
        Assert.Same(inner, exception.InnerException);
    }
}
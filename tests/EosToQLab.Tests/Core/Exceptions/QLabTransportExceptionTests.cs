using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabTransportExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new QLabTransportException("/test", inner);

        ExceptionAssertions.HasDetails(exception, "QLAB_TRANSPORT_FAILED", "/test");
        Assert.Same(inner, exception.InnerException);
    }
}
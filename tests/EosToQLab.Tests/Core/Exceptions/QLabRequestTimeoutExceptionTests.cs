using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabRequestTimeoutExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new QLabRequestTimeoutException("/test", TimeSpan.FromSeconds(10), inner);

        ExceptionAssertions.HasDetails(exception, "QLAB_REQUEST_TIMEOUT", "/test", "10");
        Assert.Same(inner, exception.InnerException);
    }
}
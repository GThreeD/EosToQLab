using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabConnectionExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new QLabConnectionException("localhost", 53000, inner);

        ExceptionAssertions.HasDetails(exception, "QLAB_CONNECTION_FAILED", "localhost", "53000");
        Assert.Same(inner, exception.InnerException);
    }
}
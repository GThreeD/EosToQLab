using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class UnexpectedApplicationExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new UnexpectedApplicationException(inner);

        ExceptionAssertions.HasDetails(exception, "UNEXPECTED_APPLICATION_ERROR", "unexpected");
        Assert.Same(inner, exception.InnerException);
    }
}
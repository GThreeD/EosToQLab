using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabConnectionClosedExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var exception = new QLabConnectionClosedException();

        ExceptionAssertions.HasDetails(exception, "QLAB_CONNECTION_CLOSED", "closed");
    }
}
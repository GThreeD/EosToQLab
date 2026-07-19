using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabAccessDeniedExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var exception = new QLabAccessDeniedException("Workspace");

        ExceptionAssertions.HasDetails(exception, "QLAB_ACCESS_DENIED", "Workspace");
    }
}
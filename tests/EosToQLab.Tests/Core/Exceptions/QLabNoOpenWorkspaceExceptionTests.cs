using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabNoOpenWorkspaceExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var exception = new QLabNoOpenWorkspaceException();

        ExceptionAssertions.HasDetails(exception, "QLAB_NO_OPEN_WORKSPACE", "no workspace");
    }
}
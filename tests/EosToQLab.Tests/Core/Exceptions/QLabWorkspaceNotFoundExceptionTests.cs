using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabWorkspaceNotFoundExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var exception = new QLabWorkspaceNotFoundException("id");

        ExceptionAssertions.HasDetails(exception, "QLAB_WORKSPACE_NOT_FOUND", "id");
    }
}
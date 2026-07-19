using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabWorkspaceSaveExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new QLabWorkspaceSaveException("Workspace", inner);

        ExceptionAssertions.HasDetails(exception, "QLAB_WORKSPACE_SAVE_FAILED", "Workspace");
        Assert.Same(inner, exception.InnerException);
    }
}
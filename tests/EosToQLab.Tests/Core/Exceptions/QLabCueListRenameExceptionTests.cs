using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabCueListRenameExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new QLabCueListRenameException("Old", "New", inner);

        ExceptionAssertions.HasDetails(exception, "QLAB_CUE_LIST_RENAME_FAILED", "Old", "New");
        Assert.Same(inner, exception.InnerException);
    }
}
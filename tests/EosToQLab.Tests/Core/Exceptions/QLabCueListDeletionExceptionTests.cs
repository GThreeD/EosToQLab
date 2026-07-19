using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabCueListDeletionExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new QLabCueListDeletionException("List", inner);

        ExceptionAssertions.HasDetails(exception, "QLAB_CUE_LIST_DELETION_FAILED", "List");
        Assert.Same(inner, exception.InnerException);
    }
}
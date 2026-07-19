using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabCueListCreationExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new QLabCueListCreationException("List", inner);

        ExceptionAssertions.HasDetails(exception, "QLAB_CUE_LIST_CREATION_FAILED", "List");
        Assert.Same(inner, exception.InnerException);
    }
}
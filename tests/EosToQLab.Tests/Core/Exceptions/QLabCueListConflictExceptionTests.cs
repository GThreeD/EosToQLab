using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabCueListConflictExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var exception = new QLabCueListConflictException("List");

        ExceptionAssertions.HasDetails(exception, "QLAB_CUE_LIST_CONFLICT", "List");
        Assert.Equal("List", exception.CueListName);
    }
}
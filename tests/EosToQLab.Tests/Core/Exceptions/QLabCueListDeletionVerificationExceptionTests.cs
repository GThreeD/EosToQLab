using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabCueListDeletionVerificationExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var exception = new QLabCueListDeletionVerificationException("List");

        ExceptionAssertions.HasDetails(exception, "QLAB_CUE_LIST_DELETION_NOT_APPLIED", "List");
    }
}
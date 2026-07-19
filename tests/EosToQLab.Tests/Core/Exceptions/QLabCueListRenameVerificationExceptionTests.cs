using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabCueListRenameVerificationExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var exception = new QLabCueListRenameVerificationException("Expected", null);

        ExceptionAssertions.HasDetails(exception, "QLAB_CUE_LIST_RENAME_NOT_APPLIED", "Expected", "<empty>");
    }
}
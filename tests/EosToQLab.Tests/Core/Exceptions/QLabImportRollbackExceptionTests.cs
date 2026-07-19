using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabImportRollbackExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new QLabImportRollbackException("temporary", inner);

        ExceptionAssertions.HasDetails(exception, "QLAB_IMPORT_ROLLBACK_FAILED", "temporary");
        Assert.Same(inner, exception.InnerException);
    }
}
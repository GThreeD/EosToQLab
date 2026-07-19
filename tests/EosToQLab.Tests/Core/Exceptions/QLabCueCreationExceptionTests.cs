using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabCueCreationExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new QLabCueCreationException("Cue", inner);

        ExceptionAssertions.HasDetails(exception, "QLAB_CUE_CREATION_FAILED", "Cue");
        Assert.Same(inner, exception.InnerException);
    }
}
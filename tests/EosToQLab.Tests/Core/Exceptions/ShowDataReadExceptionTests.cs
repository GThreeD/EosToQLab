using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class ShowDataReadExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new ShowDataReadException("show.esf3d", inner);

        ExceptionAssertions.HasDetails(exception, "EOS_SHOW_ARCHIVE_SHOWDAT_READ_FAILED", "show.esf3d");
        Assert.Same(inner, exception.InnerException);
    }
}
using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class Esf3dArchiveInvalidExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new Esf3dArchiveInvalidException("show.esf3d", inner);

        ExceptionAssertions.HasDetails(exception, "EOS_ESF3D_ARCHIVE_INVALID", "show.esf3d");
        Assert.Same(inner, exception.InnerException);
    }
}
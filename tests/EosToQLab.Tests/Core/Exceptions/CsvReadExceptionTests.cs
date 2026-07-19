using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class CsvReadExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new CsvReadException("show.csv", inner);

        ExceptionAssertions.HasDetails(exception, "EOS_CSV_READ_FAILED", "show.csv");
        Assert.Same(inner, exception.InnerException);
    }
}
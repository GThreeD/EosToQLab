using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class CsvHeaderMissingExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var exception = new CsvHeaderMissingException("show.csv");

        ExceptionAssertions.HasDetails(exception, "EOS_CSV_HEADER_MISSING", "show.csv");
    }
}
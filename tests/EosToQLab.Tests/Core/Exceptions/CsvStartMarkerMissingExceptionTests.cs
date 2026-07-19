using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class CsvStartMarkerMissingExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var exception = new CsvStartMarkerMissingException("show.csv");

        ExceptionAssertions.HasDetails(exception, "EOS_CSV_START_MARKER_MISSING", "show.csv");
    }
}
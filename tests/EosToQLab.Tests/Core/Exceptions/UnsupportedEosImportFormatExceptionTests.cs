using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class UnsupportedEosImportFormatExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var exception = new UnsupportedEosImportFormatException("show.txt");

        ExceptionAssertions.HasDetails(exception, "EOS_IMPORT_FORMAT_UNSUPPORTED", "show.txt");
    }
}
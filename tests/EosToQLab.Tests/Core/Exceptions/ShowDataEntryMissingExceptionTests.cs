using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class ShowDataEntryMissingExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var exception = new ShowDataEntryMissingException("show.esf3d");

        ExceptionAssertions.HasDetails(exception, "EOS_SHOW_ARCHIVE_SHOWDAT_MISSING", "show.esf3d");
    }
}
using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabUnexpectedReplyExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var exception = new QLabUnexpectedReplyException("/test", "error");

        ExceptionAssertions.HasDetails(exception, "QLAB_UNEXPECTED_REPLY", "/test", "error");
    }
}
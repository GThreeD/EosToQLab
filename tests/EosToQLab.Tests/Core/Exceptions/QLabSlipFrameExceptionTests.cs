using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabSlipFrameExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var exception = new QLabSlipFrameException(0xAA);

        ExceptionAssertions.HasDetails(exception, "QLAB_SLIP_FRAME_INVALID", "0xAA");
    }
}
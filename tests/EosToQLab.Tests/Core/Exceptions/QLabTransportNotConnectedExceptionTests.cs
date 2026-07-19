using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabTransportNotConnectedExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var exception = new QLabTransportNotConnectedException();

        ExceptionAssertions.HasDetails(exception, "QLAB_TRANSPORT_NOT_CONNECTED", "not connected");
    }
}
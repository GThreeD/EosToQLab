using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabNetworkPatchNotFoundExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var exception = new QLabNetworkPatchNotFoundException("Eos");

        ExceptionAssertions.HasDetails(exception, "QLAB_NETWORK_PATCH_NOT_FOUND", "Eos");
    }
}
using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabNetworkPatchTypeMismatchExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var exception = new QLabNetworkPatchTypeMismatchException("Eos", "osc");

        ExceptionAssertions.HasDetails(exception, "QLAB_NETWORK_PATCH_TYPE_MISMATCH", "Eos", "osc");
    }
}
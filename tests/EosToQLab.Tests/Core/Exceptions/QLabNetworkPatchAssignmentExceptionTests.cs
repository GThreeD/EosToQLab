using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabNetworkPatchAssignmentExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var exception = new QLabNetworkPatchAssignmentException("Cue", "Eos");

        ExceptionAssertions.HasDetails(exception, "QLAB_NETWORK_PATCH_ASSIGNMENT_FAILED", "Cue", "Eos");
    }
}
using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class QLabUnsupportedPlanItemExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var exception = new QLabUnsupportedPlanItemException("Unknown");

        ExceptionAssertions.HasDetails(exception, "QLAB_PLAN_ITEM_UNSUPPORTED", "Unknown");
    }
}
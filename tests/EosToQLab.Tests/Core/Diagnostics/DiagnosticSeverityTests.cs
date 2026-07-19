namespace EosToQLab.Tests.Core.Diagnostics;

public sealed class DiagnosticSeverityTests
{
    [Fact]
    public void Warning_is_the_only_supported_value()
    {
        Assert.Equal([DiagnosticSeverity.Warning], Enum.GetValues<DiagnosticSeverity>());
    }
}
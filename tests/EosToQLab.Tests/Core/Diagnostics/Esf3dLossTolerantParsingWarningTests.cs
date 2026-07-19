namespace EosToQLab.Tests.Core.Diagnostics;

public sealed class Esf3dLossTolerantParsingWarningTests
{
    [Fact]
    public void Exposes_stable_code_warning_severity_and_message()
    {
        var warning = new Esf3dLossTolerantParsingWarning();

        Assert.Equal("EOS_ESF3D_LOSS_TOLERANT_PARSE", warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("loss-tolerant", warning.Message, StringComparison.OrdinalIgnoreCase);
    }
}
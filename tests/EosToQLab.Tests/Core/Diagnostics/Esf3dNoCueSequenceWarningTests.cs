namespace EosToQLab.Tests.Core.Diagnostics;

public sealed class Esf3dNoCueSequenceWarningTests
{
    [Fact]
    public void Exposes_stable_code_warning_severity_and_message()
    {
        var warning = new Esf3dNoCueSequenceWarning();

        Assert.Equal("EOS_ESF3D_NO_CUE_SEQUENCE", warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("monotonic", warning.Message, StringComparison.OrdinalIgnoreCase);
    }
}
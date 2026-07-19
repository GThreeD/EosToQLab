namespace EosToQLab.Tests.Core.Diagnostics;

public sealed class EosWarningTests
{
    [Fact]
    public void Severity_is_always_warning()
    {
        EosWarning warning = new TestWarning();
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    private sealed record TestWarning : EosWarning
    {
        public override string Code => "TEST";
        public override string Message => "message";
    }
}
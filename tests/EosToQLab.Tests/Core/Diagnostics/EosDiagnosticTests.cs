namespace EosToQLab.Tests.Core.Diagnostics;

public sealed class EosDiagnosticTests
{
    [Fact]
    public void Derived_diagnostic_supplies_contract_values()
    {
        EosDiagnostic diagnostic = new TestDiagnostic();
        Assert.Equal("TEST", diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("message", diagnostic.Message);
    }

    private sealed record TestDiagnostic : EosDiagnostic
    {
        public override string Code => "TEST";
        public override DiagnosticSeverity Severity => DiagnosticSeverity.Warning;
        public override string Message => "message";
    }
}
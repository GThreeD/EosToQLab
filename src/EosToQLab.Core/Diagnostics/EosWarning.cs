namespace EosToQLab.Core.Diagnostics;

public abstract record EosWarning : EosDiagnostic
{
    public sealed override DiagnosticSeverity Severity => DiagnosticSeverity.Warning;
}
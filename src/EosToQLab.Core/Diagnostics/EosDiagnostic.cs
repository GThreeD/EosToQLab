namespace EosToQLab.Core.Diagnostics;

public abstract record EosDiagnostic
{
    public abstract string Code { get; }
    public abstract DiagnosticSeverity Severity { get; }
    public abstract string Message { get; }
}
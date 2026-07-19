using EosToQLab.Core.Diagnostics;

namespace EosToQLab.Core.Models;

public sealed record EosImportResult(
    IReadOnlyList<EosCue> Cues,
    IReadOnlyList<EosDiagnostic> Diagnostics,
    EosSourceKind SourceKind,
    string SourceDescription);
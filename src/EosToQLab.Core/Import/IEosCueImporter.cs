using EosToQLab.Core.Models;

namespace EosToQLab.Core.Import;

public interface IEosCueImporter
{
    EosSourceKind SourceKind { get; }
    bool CanImport(string fileName);
    Task<EosImportResult> ImportAsync(EosImportRequest request, CancellationToken cancellationToken = default);
}
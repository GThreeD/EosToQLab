namespace EosToQLab.Tests.TestDoubles;

internal sealed class FakeEosCueImporter(EosSourceKind sourceKind, Func<string, bool> canImport) : IEosCueImporter
{
    public EosSourceKind SourceKind { get; } = sourceKind;

    public bool CanImport(string fileName)
    {
        return canImport(fileName);
    }

    public Task<EosImportResult> ImportAsync(EosImportRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new EosImportResult([], [], SourceKind, request.FileName));
    }
}
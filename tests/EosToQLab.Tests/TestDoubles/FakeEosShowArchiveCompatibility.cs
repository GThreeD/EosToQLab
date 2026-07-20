namespace EosToQLab.Tests.TestDoubles;

internal sealed class FakeEosShowArchiveCompatibility : IEosShowArchiveCompatibility
{
    private readonly Func<string?, string?, bool> _isCovered;

    public FakeEosShowArchiveCompatibility(Func<string?, string?, bool>? isCovered = null)
    {
        _isCovered = isCovered ?? ((_, _) => true);
    }

    public bool IsCovered(string? format, string? version) => _isCovered(format, version);
}

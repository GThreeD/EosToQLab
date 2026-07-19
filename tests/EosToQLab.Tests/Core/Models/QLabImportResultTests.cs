namespace EosToQLab.Tests.Core.Models;

public sealed class QLabImportResultTests
{
    [Fact]
    public void Stores_constructor_values()
    {
        var value = new QLabImportResult("w", "c", 2, 1, true);
        Assert.Equal(("w", "c", 2, 1, true),
            (value.WorkspaceId, value.CueListId, value.NetworkCueCount, value.MemoCueCount,
                value.ReplacedExistingCueList));
    }
}
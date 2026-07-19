namespace EosToQLab.Tests.Infrastructure.QLab;

public sealed class QLabWorkspacePropertyTests
{
    [Fact]
    public void Defines_current_cue_list_property()
    {
        Assert.Equal([QLabWorkspaceProperty.CurrentCueListId], Enum.GetValues<QLabWorkspaceProperty>());
    }
}
namespace EosToQLab.Tests.Core.Models;

public sealed class SceneTextImportModeTests
{
    [Fact]
    public void Defines_expected_values()
    {
        Assert.NotEmpty(Enum.GetValues<SceneTextImportMode>());
    }
}
using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Models;

public sealed class QLabImportOptionsTests
{
    [Fact]
    public void Defaults_are_safe()
    {
        var options = TestData.Options();
        Assert.Equal(SceneTextImportMode.MemoCue, options.SceneTextMode);
        Assert.Equal(FollowedCueImportMode.Exclude, options.FollowedCueMode);
        Assert.Equal(CueListConflictPolicy.Fail, options.ConflictPolicy);
        Assert.False(options.ExplicitReplacementConsent);
        Assert.True(options.SaveWorkspaceAfterImport);
        Assert.Null(options.Passcode);
    }
}
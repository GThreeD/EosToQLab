using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Models;

public sealed class CuePreviewRowTests
{
    [Fact]
    public void FromCue_copies_display_values_and_ToCue_applies_trimmed_edits()
    {
        var source = TestData.Cue(4, "2", "Old", null, "F1", "Scene", 3, false);
        var row = CuePreviewRow.FromCue(source);

        Assert.Equal(4, row.SourceOrder);
        Assert.Equal("3/2", row.DisplayCueNumber);
        Assert.Equal("F1", row.Follow);
        Assert.False(row.IsSelected);
        Assert.Equal("Old", row.Label);
        Assert.Equal(string.Empty, row.Notes);
        Assert.Equal("Scene", row.Scene);

        row.IsSelected = true;
        row.Label = " New ";
        row.Notes = "   ";
        row.Scene = " Scene 2 ";
        var updated = row.ToCue();

        Assert.True(updated.ImportEnabled);
        Assert.Equal("New", updated.Label);
        Assert.Null(updated.CueNotes);
        Assert.Equal("Scene 2", updated.SceneText);
        Assert.Equal(source.CueNumber, updated.CueNumber);
    }

    [Fact]
    public void FromCue_rejects_null()
    {
        Assert.Throws<ArgumentNullException>(() => CuePreviewRow.FromCue(null!));
    }
}
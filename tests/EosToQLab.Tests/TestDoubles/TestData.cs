namespace EosToQLab.Tests.TestDoubles;

internal static class TestData
{
    public static EosCue Cue(
        int order = 0,
        string number = "1",
        string? label = null,
        string? notes = null,
        string? follow = null,
        string? scene = null,
        int listNumber = 1,
        bool importEnabled = true)
    {
        return new EosCue
        {
            SourceOrder = order,
            ListNumber = listNumber,
            CueNumber = number,
            Label = label,
            CueNotes = notes,
            Follow = follow,
            SceneText = scene,
            ImportEnabled = importEnabled,
            SourceKind = EosSourceKind.Csv
        };
    }

    public static QLabImportOptions Options()
    {
        return new QLabImportOptions
        {
            WorkspaceId = "workspace",
            CueListName = "Imported",
            NetworkPatchId = "patch-id",
            NetworkPatchName = "Eos",
            SceneTextMode = SceneTextImportMode.MemoCue,
            FollowedCueMode = FollowedCueImportMode.Exclude
        };
    }

    public static string FixturePath(params string[] parts)
    {
        var all = new[] { AppContext.BaseDirectory, "Fixtures" }.Concat(parts).ToArray();
        return Path.Combine(all);
    }
}
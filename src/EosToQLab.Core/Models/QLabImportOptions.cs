namespace EosToQLab.Core.Models;

public sealed record QLabImportOptions
{
    public required string WorkspaceId { get; init; }
    public string? Passcode { get; init; }
    public required string CueListName { get; init; }
    public string CueNamePrefix { get; init; } = "EOS";
    public string NetworkPatchName { get; init; } = "Patch 1";
    public SceneTextImportMode SceneTextMode { get; init; } = SceneTextImportMode.MemoCueAndNotes;
    public bool SkipCueAfterFollowOrHang { get; init; } = true;
    public CueListConflictPolicy ConflictPolicy { get; init; } = CueListConflictPolicy.Fail;
    public bool ExplicitReplacementConsent { get; init; }
    public bool SaveWorkspaceAfterImport { get; init; } = true;
}

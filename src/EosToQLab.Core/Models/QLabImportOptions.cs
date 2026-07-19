namespace EosToQLab.Core.Models;

public sealed record QLabImportOptions
{
    public required string WorkspaceId { get; init; }
    public string? Passcode { get; init; }
    public required string CueListName { get; init; }
    public required string NetworkPatchId { get; init; }
    public required string NetworkPatchName { get; init; }
    public SceneTextImportMode SceneTextMode { get; init; } = SceneTextImportMode.MemoCue;
    public FollowedCueImportMode FollowedCueMode { get; init; } = FollowedCueImportMode.Exclude;
    public CueListConflictPolicy ConflictPolicy { get; init; } = CueListConflictPolicy.Fail;
    public bool ExplicitReplacementConsent { get; init; }
    public bool SaveWorkspaceAfterImport { get; init; } = true;
}
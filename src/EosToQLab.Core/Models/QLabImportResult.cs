namespace EosToQLab.Core.Models;

public sealed record QLabImportResult(
    string WorkspaceId,
    string CueListId,
    int NetworkCueCount,
    int MemoCueCount,
    bool ReplacedExistingCueList);

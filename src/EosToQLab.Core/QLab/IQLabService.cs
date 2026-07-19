using EosToQLab.Core.Models;

namespace EosToQLab.Core.QLab;

public interface IQLabService
{
    Task<IReadOnlyList<QLabWorkspace>> GetOpenWorkspacesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QLabCueList>> GetCueListsAsync(
        string workspaceId,
        string? passcode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QLabNetworkPatch>> GetNetworkPatchesAsync(
        string workspaceId,
        string? passcode,
        CancellationToken cancellationToken = default);

    Task<QLabImportResult> ImportAsync(
        IReadOnlyList<EosCue> cues,
        QLabImportOptions options,
        CancellationToken cancellationToken = default);
}
using EosToQLab.Core.Models;

namespace EosToQLab.Infrastructure.QLab;

public interface IQLabOscSession : IAsyncDisposable
{
    QLabWorkspace Workspace { get; }

    Task<IReadOnlyList<QLabCueList>> GetCueListsAsync(
        CancellationToken cancellationToken = default);

    Task<QLabNetworkPatch> FindNetworkPatchAsync(
        string patchName,
        CancellationToken cancellationToken = default);

    Task<string?> GetCurrentCueListIdAsync(
        CancellationToken cancellationToken = default);

    Task<string> CreateCueAsync(
        QLabCueType cueType,
        string cueName,
        CancellationToken cancellationToken = default);

    Task SetCuePropertyAsync(
        string cueId,
        QLabCueProperty cueProperty,
        object? value,
        CancellationToken cancellationToken = default);

    Task SetWorkspacePropertyAsync(
        QLabWorkspaceProperty cueProperty,
        object? value,
        CancellationToken cancellationToken = default);

    Task SetNetworkParameterAsync(
        string cueId,
        int parameterIndex,
        string value,
        CancellationToken cancellationToken = default);

    Task<string?> QueryCuePropertyAsync(
        string cueId,
        QLabCueProperty cueProperty,
        CancellationToken cancellationToken = default);

    Task RenameCueListAsync(
        string cueListId,
        string currentName,
        string targetName,
        CancellationToken cancellationToken = default);

    Task DeleteCueListAsync(
        string cueListId,
        string cueListName,
        CancellationToken cancellationToken = default);

    Task SaveWorkspaceAsync(CancellationToken cancellationToken = default);

    Task UndoAsync(CancellationToken cancellationToken = default);
}
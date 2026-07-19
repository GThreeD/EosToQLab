using EosToQLab.Core.Models;

namespace EosToQLab.Infrastructure.QLab;

public interface IQLabOscService
{
    Task<IReadOnlyList<QLabWorkspace>> GetOpenWorkspacesAsync(
        CancellationToken cancellationToken = default);

    Task<IQLabOscSession> ConnectWorkspaceAsync(
        string workspaceId,
        string? passcode,
        CancellationToken cancellationToken = default);
}
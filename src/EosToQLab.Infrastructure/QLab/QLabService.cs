using EosToQLab.Core.Models;
using EosToQLab.Core.QLab;
using EosToQLab.Infrastructure.QLab.Workflow;

namespace EosToQLab.Infrastructure.QLab;

public sealed class QLabService(
    IQLabOscService oscService,
    QLabImportWorkflow importWorkflow) : IQLabService
{
    public Task<IReadOnlyList<QLabWorkspace>> GetOpenWorkspacesAsync(
        CancellationToken cancellationToken = default) =>
        oscService.GetOpenWorkspacesAsync(cancellationToken);

    public async Task<IReadOnlyList<QLabCueList>> GetCueListsAsync(
        string workspaceId,
        string? passcode,
        CancellationToken cancellationToken = default)
    {
        await using var session = await oscService.ConnectWorkspaceAsync(
            workspaceId,
            passcode,
            cancellationToken);
        return await session.GetCueListsAsync(cancellationToken);
    }

    public Task<QLabImportResult> ImportAsync(
        IReadOnlyList<EosCue> cues,
        QLabImportOptions options,
        CancellationToken cancellationToken = default) =>
        importWorkflow.ExecuteAsync(cues, options, cancellationToken);
}

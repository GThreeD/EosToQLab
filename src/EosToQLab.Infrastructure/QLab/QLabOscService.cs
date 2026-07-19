using EosToQLab.Core.Exceptions;
using EosToQLab.Core.Models;
using EosToQLab.Infrastructure.QLab.Osc;

namespace EosToQLab.Infrastructure.QLab;

public sealed class QLabOscService : IQLabOscService
{
    private readonly IQLabOscTransportFactory _transportFactory;

    public QLabOscService() : this(new QLabTcpOscTransportFactory())
    {
    }

    internal QLabOscService(IQLabOscTransportFactory transportFactory)
    {
        _transportFactory = transportFactory;
    }

    public async Task<IReadOnlyList<QLabWorkspace>> GetOpenWorkspacesAsync(
        CancellationToken cancellationToken = default)
    {
        await using var transport = _transportFactory.Create();
        await transport.ConnectAsync(cancellationToken);
        using var reply = await transport.SendAsync(
            new OscMessage(QLabProtocol.Addresses.Workspaces),
            cancellationToken);
        reply.EnsureOk("QLab");

        var workspaces = QLabJsonParser.ParseWorkspaces(reply.Data);
        if (workspaces.Count == 0) throw new QLabNoOpenWorkspaceException();

        return workspaces;
    }

    public async Task<IQLabOscSession> ConnectWorkspaceAsync(
        string workspaceId,
        string? passcode,
        CancellationToken cancellationToken = default)
    {
        var transport = _transportFactory.Create();
        try
        {
            await transport.ConnectAsync(cancellationToken);
            var workspace = await FindWorkspaceAsync(transport, workspaceId, cancellationToken);
            await ConnectWorkspaceAsync(transport, workspace, passcode, cancellationToken);

            var session = new QLabOscSession(transport, workspace);
            await session.EnableAlwaysReplyAsync(cancellationToken);
            return session;
        }
        catch
        {
            await transport.DisposeAsync();
            throw;
        }
    }

    private static async Task<QLabWorkspace> FindWorkspaceAsync(
        IQLabOscTransport transport,
        string workspaceId,
        CancellationToken cancellationToken)
    {
        using var reply = await transport.SendAsync(
            new OscMessage(QLabProtocol.Addresses.Workspaces),
            cancellationToken);
        reply.EnsureOk("QLab");

        return QLabJsonParser.ParseWorkspaces(reply.Data)
                   .FirstOrDefault(candidate =>
                       string.Equals(candidate.Id, workspaceId, StringComparison.OrdinalIgnoreCase))
               ?? throw new QLabWorkspaceNotFoundException(workspaceId);
    }

    private static async Task ConnectWorkspaceAsync(
        IQLabOscTransport transport,
        QLabWorkspace workspace,
        string? passcode,
        CancellationToken cancellationToken)
    {
        var address = QLabProtocol.Addresses.Workspace(
            workspace.Id,
            QLabProtocol.WorkspaceCommands.Connect);
        using var reply = string.IsNullOrEmpty(passcode)
            ? await transport.SendAsync(new OscMessage(address), cancellationToken)
            : await transport.SendAsync(new OscMessage(address, passcode), cancellationToken);
        reply.EnsureOk(workspace.Name);
    }
}
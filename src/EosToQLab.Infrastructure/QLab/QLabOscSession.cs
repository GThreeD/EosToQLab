using EosToQLab.Core.Exceptions;
using EosToQLab.Core.Models;
using EosToQLab.Infrastructure.QLab.Osc;

namespace EosToQLab.Infrastructure.QLab;

public sealed class QLabOscSession : IQLabOscSession
{
    private readonly IQLabOscTransport _transport;

    internal QLabOscSession(
        IQLabOscTransport transport,
        QLabWorkspace workspace)
    {
        _transport = transport;
        Workspace = workspace;
    }

    public QLabWorkspace Workspace { get; }

    public async Task<IReadOnlyList<QLabCueList>> GetCueListsAsync(
        CancellationToken cancellationToken = default)
    {
        using var reply = await SendWorkspaceCommandAsync(
            QLabProtocol.WorkspaceCommands.CueListsShallow,
            cancellationToken);
        return QLabJsonParser.ParseCueLists(reply.Data);
    }

    public async Task<IReadOnlyList<QLabNetworkPatch>> GetNetworkPatchesAsync(
        CancellationToken cancellationToken = default)
    {
        using var reply = await SendWorkspaceCommandAsync(
            QLabProtocol.WorkspaceCommands.NetworkPatchList,
            cancellationToken);
        return QLabJsonParser.ParseNetworkPatches(reply.Data);
    }

    public Task<string?> GetCurrentCueListIdAsync(CancellationToken cancellationToken = default)
    {
        return QueryWorkspacePropertyAsync(QLabWorkspaceProperty.CurrentCueListId, cancellationToken);
    }

    public async Task<string> CreateCueAsync(
        QLabCueType cueType,
        string cueName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var reply = await SendWorkspaceCommandAsync(
                QLabProtocol.WorkspaceCommands.NewCue,
                cancellationToken,
                QLabProtocol.CueTypeName(cueType));
            var cueId = QLabJsonParser.GetString(reply.Data);
            if (string.IsNullOrWhiteSpace(cueId))
                throw new QLabUnexpectedReplyException(reply.Address, reply.Root.GetRawText());

            return cueId;
        }
        catch (EosToQLabException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (cueType == QLabCueType.CueList) throw new QLabCueListCreationException(cueName, exception);

            throw new QLabCueCreationException(cueName, exception);
        }
    }

    public async Task SetCuePropertyAsync(
        string cueId,
        QLabCueProperty cueProperty,
        object? value,
        CancellationToken cancellationToken = default)
    {
        using var reply = await SendAsync(
            QLabProtocol.Addresses.Cue(
                Workspace.Id,
                cueId,
                QLabProtocol.CuePropertyName(cueProperty)),
            cancellationToken,
            value);
    }

    public async Task SetWorkspacePropertyAsync(
        QLabWorkspaceProperty cueProperty,
        object? value,
        CancellationToken cancellationToken = default)
    {
        using var reply = await SendWorkspaceCommandAsync(
            QLabProtocol.WorkspacePropertyName(cueProperty),
            cancellationToken,
            value);
    }

    public Task SetNetworkParameterAsync(
        string cueId,
        int parameterIndex,
        string value,
        CancellationToken cancellationToken = default)
    {
        return SetCuePropertyByProtocolNameAsync(
            cueId,
            QLabProtocol.NetworkParameterProperty(parameterIndex),
            value,
            cancellationToken);
    }

    public Task<string?> QueryCuePropertyAsync(
        string cueId,
        QLabCueProperty cueProperty,
        CancellationToken cancellationToken = default)
    {
        return QueryStringAsync(
            QLabProtocol.Addresses.Cue(
                Workspace.Id,
                cueId,
                QLabProtocol.CuePropertyName(cueProperty)),
            cancellationToken);
    }

    public async Task RenameCueListAsync(
        string cueListId,
        string currentName,
        string targetName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await SetCuePropertyAsync(
                cueListId,
                QLabCueProperty.Name,
                targetName,
                cancellationToken);
            var appliedName = await QueryCuePropertyAsync(
                cueListId,
                QLabCueProperty.Name,
                cancellationToken);
            if (!string.Equals(appliedName, targetName, StringComparison.Ordinal))
                throw new QLabCueListRenameVerificationException(targetName, appliedName);
        }
        catch (QLabCueListRenameVerificationException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new QLabCueListRenameException(currentName, targetName, exception);
        }
    }

    public async Task DeleteCueListAsync(
        string cueListId,
        string cueListName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var reply = await SendWorkspaceCommandAsync(
                QLabProtocol.WorkspaceCommands.DeleteById(cueListId),
                cancellationToken);
            var remainingCueLists = await GetCueListsAsync(cancellationToken);
            if (remainingCueLists.Any(list =>
                    string.Equals(list.Id, cueListId, StringComparison.OrdinalIgnoreCase)))
                throw new QLabCueListDeletionVerificationException(cueListName);
        }
        catch (QLabCueListDeletionVerificationException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new QLabCueListDeletionException(cueListName, exception);
        }
    }

    public async Task SaveWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var reply = await SendWorkspaceCommandAsync(
                QLabProtocol.WorkspaceCommands.Save,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new QLabWorkspaceSaveException(Workspace.Name, exception);
        }
    }

    public async Task UndoAsync(CancellationToken cancellationToken = default)
    {
        using var reply = await SendWorkspaceCommandAsync(
            QLabProtocol.WorkspaceCommands.Undo,
            cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _transport.DisposeAsync();
    }

    internal async Task EnableAlwaysReplyAsync(CancellationToken cancellationToken)
    {
        using var reply = await SendAsync(
            QLabProtocol.Addresses.AlwaysReply,
            cancellationToken,
            1);
    }

    private Task<string?> QueryWorkspacePropertyAsync(
        QLabWorkspaceProperty property,
        CancellationToken cancellationToken)
    {
        return QueryStringAsync(
            QLabProtocol.Addresses.Workspace(
                Workspace.Id,
                QLabProtocol.WorkspacePropertyName(property)),
            cancellationToken);
    }

    private async Task SetCuePropertyByProtocolNameAsync(
        string cueId,
        string property,
        object? value,
        CancellationToken cancellationToken)
    {
        using var reply = await SendAsync(
            QLabProtocol.Addresses.Cue(Workspace.Id, cueId, property),
            cancellationToken,
            value);
    }

    private async Task<string?> QueryStringAsync(
        string address,
        CancellationToken cancellationToken)
    {
        using var reply = await SendAsync(address, cancellationToken);
        return QLabJsonParser.GetString(reply.Data);
    }

    private Task<QLabOscReply> SendWorkspaceCommandAsync(
        string command,
        CancellationToken cancellationToken,
        params object?[] arguments)
    {
        return SendAsync(
            QLabProtocol.Addresses.Workspace(Workspace.Id, command),
            cancellationToken,
            arguments);
    }

    private async Task<QLabOscReply> SendAsync(
        string address,
        CancellationToken cancellationToken,
        params object?[] arguments)
    {
        var reply = await _transport.SendAsync(new OscMessage(address, arguments), cancellationToken);
        try
        {
            reply.EnsureOk(Workspace.Name);
            return reply;
        }
        catch
        {
            reply.Dispose();
            throw;
        }
    }
}
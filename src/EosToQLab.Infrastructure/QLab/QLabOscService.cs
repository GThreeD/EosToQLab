using System.Text.Json;
using EosToQLab.Core.Exceptions;
using EosToQLab.Core.Models;
using EosToQLab.Core.Planning;
using EosToQLab.Core.QLab;
using EosToQLab.Infrastructure.QLab.Osc;

namespace EosToQLab.Infrastructure.QLab;

public sealed class QLabOscService(IQLabImportPlanBuilder planBuilder) : IQLabService
{
    public async Task<IReadOnlyList<QLabWorkspace>> GetOpenWorkspacesAsync(
        CancellationToken cancellationToken = default)
    {
        await using var transport = new QLabTcpOscTransport();
        await transport.ConnectAsync(cancellationToken);
        using var reply = await transport.SendAsync(new OscMessage("/workspaces"), cancellationToken);
        reply.EnsureOk("QLab");
        var workspaces = ParseWorkspaces(reply.Data);
        if (workspaces.Count == 0)
        {
            throw new QLabNoOpenWorkspaceException();
        }
        return workspaces;
    }

    public async Task<IReadOnlyList<QLabCueList>> GetCueListsAsync(
        string workspaceId,
        string? passcode,
        CancellationToken cancellationToken = default)
    {
        await using var transport = new QLabTcpOscTransport();
        await transport.ConnectAsync(cancellationToken);
        var workspace = await FindAndConnectWorkspaceAsync(transport, workspaceId, passcode, cancellationToken);
        await EnableAlwaysReplyAsync(transport, workspace, cancellationToken);
        using var reply = await SendAsync(
            transport,
            WorkspaceAddress(workspace.Id, "cueLists/shallow"),
            workspace.Name,
            cancellationToken);
        return ParseCueLists(reply.Data);
    }

    public async Task<QLabImportResult> ImportAsync(
        IReadOnlyList<EosCue> cues,
        QLabImportOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cues);
        ArgumentNullException.ThrowIfNull(options);

        await using var transport = new QLabTcpOscTransport();
        await transport.ConnectAsync(cancellationToken);
        var workspace = await FindAndConnectWorkspaceAsync(
            transport,
            options.WorkspaceId,
            options.Passcode,
            cancellationToken);
        await EnableAlwaysReplyAsync(transport, workspace, cancellationToken);

        var cueLists = await GetCueListsInternalAsync(transport, workspace, cancellationToken);
        var conflict = cueLists.FirstOrDefault(list =>
            string.Equals(list.Name, options.CueListName, StringComparison.OrdinalIgnoreCase));
        if (conflict is not null
            && (options.ConflictPolicy != CueListConflictPolicy.ReplaceWithExplicitConsent
                || !options.ExplicitReplacementConsent))
        {
            throw new QLabCueListConflictException(options.CueListName);
        }

        var networkPatch = await FindNetworkPatchAsync(
            transport,
            workspace,
            options.NetworkPatchName,
            cancellationToken);
        if (!string.IsNullOrWhiteSpace(networkPatch.Type)
            && !networkPatch.Type.Contains("eos", StringComparison.OrdinalIgnoreCase))
        {
            throw new QLabNetworkPatchTypeMismatchException(networkPatch.Name, networkPatch.Type);
        }

        var plan = planBuilder.Build(cues, options);
        var originalCueListId = await QueryStringAsync(
            transport,
            WorkspaceAddress(workspace.Id, "currentCueListID"),
            workspace.Name,
            cancellationToken);

        string? temporaryCueListId = null;
        string? conflictBackupName = null;
        var conflictRenamed = false;
        var conflictDeleted = false;
        var preDeletionSaveCompleted = false;
        try
        {
            var temporaryName = $"EosToQLab temporary {Guid.NewGuid():N}";
            temporaryCueListId = await CreateCueAsync(
                transport,
                workspace,
                "cue list",
                temporaryName,
                cancellationToken);
            await RenameCueListAsync(
                transport,
                workspace,
                temporaryCueListId,
                temporaryName,
                temporaryName,
                cancellationToken);
            await SetWorkspacePropertyAsync(
                transport,
                workspace,
                "currentCueListID",
                temporaryCueListId,
                cancellationToken);

            foreach (var item in plan.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                switch (item)
                {
                    case QLabMemoCuePlan memo:
                        await CreateMemoCueAsync(transport, workspace, memo, cancellationToken);
                        break;
                    case QLabNetworkCuePlan network:
                        await CreateNetworkCueAsync(
                            transport,
                            workspace,
                            networkPatch,
                            network,
                            cancellationToken);
                        break;
                    default:
                        throw new QLabUnsupportedPlanItemException(item.GetType().Name);
                }
            }

            if (conflict is not null)
            {
                conflictBackupName = $"{conflict.Name} (EosToQLab backup {Guid.NewGuid():N})";
                await RenameCueListAsync(
                    transport,
                    workspace,
                    conflict.Id,
                    conflict.Name,
                    conflictBackupName,
                    cancellationToken);
                conflictRenamed = true;
            }

            await RenameCueListAsync(
                transport,
                workspace,
                temporaryCueListId,
                temporaryName,
                options.CueListName,
                cancellationToken);
            await SetWorkspacePropertyAsync(
                transport,
                workspace,
                "currentCueListID",
                temporaryCueListId,
                cancellationToken);

            // For replacement, save a recoverable state containing both the new list and
            // the renamed old list before the old list is deleted.
            if (conflict is not null && options.SaveWorkspaceAfterImport)
            {
                await SaveWorkspaceAsync(transport, workspace, cancellationToken);
                preDeletionSaveCompleted = true;
            }

            if (conflict is not null)
            {
                await DeleteCueListAsync(
                    transport,
                    workspace,
                    conflict.Id,
                    conflictBackupName ?? conflict.Name,
                    cancellationToken);
                conflictDeleted = true;
            }

            if (options.SaveWorkspaceAfterImport)
            {
                await SaveWorkspaceAsync(transport, workspace, cancellationToken);
            }

            return new QLabImportResult(
                workspace.Id,
                temporaryCueListId,
                plan.NetworkCueCount,
                plan.MemoCueCount,
                conflict is not null);
        }
        catch (Exception importException) when (temporaryCueListId is not null)
        {
            try
            {
                if (conflictDeleted)
                {
                    using var undoReply = await SendAsync(
                        transport,
                        WorkspaceAddress(workspace.Id, "undo"),
                        workspace.Name,
                        CancellationToken.None);
                    conflictDeleted = false;
                }

                // Free the requested final name before restoring a renamed conflicting list.
                await RenameCueListAsync(
                    transport,
                    workspace,
                    temporaryCueListId,
                    options.CueListName,
                    $"EosToQLab failed import {Guid.NewGuid():N}",
                    CancellationToken.None);

                if (conflict is not null && conflictRenamed)
                {
                    await RenameCueListAsync(
                        transport,
                        workspace,
                        conflict.Id,
                        conflictBackupName ?? conflict.Name,
                        conflict.Name,
                        CancellationToken.None);
                }

                if (!string.IsNullOrWhiteSpace(originalCueListId))
                {
                    await SetWorkspacePropertyAsync(
                        transport,
                        workspace,
                        "currentCueListID",
                        originalCueListId,
                        CancellationToken.None);
                }

                await DeleteCueListAsync(
                    transport,
                    workspace,
                    temporaryCueListId,
                    "failed temporary import",
                    CancellationToken.None);

                if (preDeletionSaveCompleted && options.SaveWorkspaceAfterImport)
                {
                    await SaveWorkspaceAsync(transport, workspace, CancellationToken.None);
                }
            }
            catch (Exception rollbackException)
            {
                throw new QLabImportRollbackException(
                    temporaryCueListId,
                    new AggregateException(importException, rollbackException));
            }

            throw;
        }
    }

    private static async Task RenameCueListAsync(
        IQLabOscTransport transport,
        QLabWorkspace workspace,
        string cueListId,
        string currentName,
        string targetName,
        CancellationToken cancellationToken)
    {
        try
        {
            await SetCuePropertyAsync(
                transport,
                workspace,
                cueListId,
                "name",
                targetName,
                cancellationToken);
            var appliedName = await QueryStringAsync(
                transport,
                CueAddress(workspace.Id, cueListId, "name"),
                workspace.Name,
                cancellationToken);
            if (!string.Equals(appliedName, targetName, StringComparison.Ordinal))
            {
                throw new QLabCueListRenameVerificationException(targetName, appliedName);
            }
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

    private static async Task DeleteCueListAsync(
        IQLabOscTransport transport,
        QLabWorkspace workspace,
        string cueListId,
        string cueListName,
        CancellationToken cancellationToken)
    {
        try
        {
            using var reply = await SendAsync(
                transport,
                WorkspaceAddress(workspace.Id, $"delete_id/{cueListId}"),
                workspace.Name,
                cancellationToken);
            var remainingCueLists = await GetCueListsInternalAsync(
                transport,
                workspace,
                cancellationToken);
            if (remainingCueLists.Any(list =>
                    string.Equals(list.Id, cueListId, StringComparison.OrdinalIgnoreCase)))
            {
                throw new QLabCueListDeletionVerificationException(cueListName);
            }
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

    private static async Task SaveWorkspaceAsync(
        IQLabOscTransport transport,
        QLabWorkspace workspace,
        CancellationToken cancellationToken)
    {
        try
        {
            using var reply = await SendAsync(
                transport,
                WorkspaceAddress(workspace.Id, "save"),
                workspace.Name,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new QLabWorkspaceSaveException(workspace.Name, exception);
        }
    }

    private static async Task CreateMemoCueAsync(
        IQLabOscTransport transport,
        QLabWorkspace workspace,
        QLabMemoCuePlan memo,
        CancellationToken cancellationToken)
    {
        try
        {
            var cueId = await CreateCueAsync(transport, workspace, "memo", memo.Name, cancellationToken);
            await SetCuePropertyAsync(transport, workspace, cueId, "name", memo.Name, cancellationToken);
            await SetCuePropertyAsync(transport, workspace, cueId, "number", string.Empty, cancellationToken);
            if (!string.IsNullOrWhiteSpace(memo.Notes))
            {
                await SetCuePropertyAsync(transport, workspace, cueId, "notes", memo.Notes, cancellationToken);
            }
        }
        catch (EosToQLabException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new QLabCueCreationException(memo.Name, exception);
        }
    }

    private static async Task CreateNetworkCueAsync(
        IQLabOscTransport transport,
        QLabWorkspace workspace,
        QLabNetworkPatch networkPatch,
        QLabNetworkCuePlan cue,
        CancellationToken cancellationToken)
    {
        try
        {
            var cueId = await CreateCueAsync(transport, workspace, "network", cue.Name, cancellationToken);
            await SetCuePropertyAsync(transport, workspace, cueId, "name", cue.Name, cancellationToken);
            await SetCuePropertyAsync(transport, workspace, cueId, "number", cue.CueNumber, cancellationToken);
            if (!string.IsNullOrWhiteSpace(cue.Notes))
            {
                await SetCuePropertyAsync(transport, workspace, cueId, "notes", cue.Notes, cancellationToken);
            }
            await SetCuePropertyAsync(
                transport,
                workspace,
                cueId,
                "networkPatchID",
                networkPatch.Id,
                cancellationToken);
            
            await SetNetworkParameterAsync(transport, workspace, cueId, 0, "Cues", cancellationToken);
            await SetNetworkParameterAsync(transport, workspace, cueId, 1, "No", cancellationToken);
            await SetNetworkParameterAsync(transport, workspace, cueId, 2, "Run cue in specific list", cancellationToken);
            await SetNetworkParameterAsync(transport, workspace, cueId, 3, cue.ListNumber, cancellationToken);
            await SetNetworkParameterAsync(transport, workspace, cueId, 4, cue.CueNumber, cancellationToken);

            var appliedPatchId = await QueryStringAsync(
                transport,
                CueAddress(workspace.Id, cueId, "networkPatchID"),
                workspace.Name,
                cancellationToken);
            if (!string.Equals(appliedPatchId, networkPatch.Id, StringComparison.OrdinalIgnoreCase))
            {
                throw new QLabNetworkPatchAssignmentException(cue.Name, networkPatch.Name);
            }
        }
        catch (EosToQLabException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new QLabCueCreationException(cue.Name, exception);
        }
    }

    private static async Task<string> CreateCueAsync(
        IQLabOscTransport transport,
        QLabWorkspace workspace,
        string cueType,
        string cueName,
        CancellationToken cancellationToken)
    {
        try
        {
            using var reply = await SendAsync(
                transport,
                WorkspaceAddress(workspace.Id, "new"),
                workspace.Name,
                cancellationToken,
                cueType);
            var cueId = GetString(reply.Data);
            if (string.IsNullOrWhiteSpace(cueId))
            {
                throw new QLabUnexpectedReplyException(reply.Address, reply.Root.GetRawText());
            }
            return cueId;
        }
        catch (EosToQLabException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (string.Equals(cueType, "cue list", StringComparison.OrdinalIgnoreCase))
            {
                throw new QLabCueListCreationException(cueName, exception);
            }
            throw new QLabCueCreationException(cueName, exception);
        }
    }

    private static async Task SetCuePropertyAsync(
        IQLabOscTransport transport,
        QLabWorkspace workspace,
        string cueId,
        string property,
        object? value,
        CancellationToken cancellationToken)
    {
        using var reply = await SendAsync(
            transport,
            CueAddress(workspace.Id, cueId, property),
            workspace.Name,
            cancellationToken,
            value);
    }

    private static async Task SetWorkspacePropertyAsync(
        IQLabOscTransport transport,
        QLabWorkspace workspace,
        string property,
        object? value,
        CancellationToken cancellationToken)
    {
        using var reply = await SendAsync(
            transport,
            WorkspaceAddress(workspace.Id, property),
            workspace.Name,
            cancellationToken,
            value);
    }
    
    private static Task SetNetworkParameterAsync(
        IQLabOscTransport transport,
        QLabWorkspace workspace,
        string cueId,
        int parameterIndex,
        string value,
        CancellationToken cancellationToken) =>
        SetCuePropertyAsync(
            transport,
            workspace,
            cueId,
            $"parameterValue/{parameterIndex}",
            value,
            cancellationToken);

    private static async Task<QLabWorkspace> FindAndConnectWorkspaceAsync(
        IQLabOscTransport transport,
        string workspaceId,
        string? passcode,
        CancellationToken cancellationToken)
    {
        using var workspacesReply = await transport.SendAsync(new OscMessage("/workspaces"), cancellationToken);
        workspacesReply.EnsureOk("QLab");
        var workspace = ParseWorkspaces(workspacesReply.Data)
            .FirstOrDefault(candidate => string.Equals(candidate.Id, workspaceId, StringComparison.OrdinalIgnoreCase))
            ?? throw new QLabWorkspaceNotFoundException(workspaceId);

        using var connectReply = string.IsNullOrEmpty(passcode)
            ? await transport.SendAsync(new OscMessage(WorkspaceAddress(workspace.Id, "connect")), cancellationToken)
            : await transport.SendAsync(new OscMessage(WorkspaceAddress(workspace.Id, "connect"), passcode), cancellationToken);
        connectReply.EnsureOk(workspace.Name);
        return workspace;
    }

    private static async Task EnableAlwaysReplyAsync(
        IQLabOscTransport transport,
        QLabWorkspace workspace,
        CancellationToken cancellationToken)
    {
        using var reply = await SendAsync(
            transport,
            "/alwaysReply",
            workspace.Name,
            cancellationToken,
            1);
    }

    private static async Task<IReadOnlyList<QLabCueList>> GetCueListsInternalAsync(
        IQLabOscTransport transport,
        QLabWorkspace workspace,
        CancellationToken cancellationToken)
    {
        using var reply = await SendAsync(
            transport,
            WorkspaceAddress(workspace.Id, "cueLists/shallow"),
            workspace.Name,
            cancellationToken);
        return ParseCueLists(reply.Data);
    }

    private static async Task<QLabNetworkPatch> FindNetworkPatchAsync(
        IQLabOscTransport transport,
        QLabWorkspace workspace,
        string patchName,
        CancellationToken cancellationToken)
    {
        using var reply = await SendAsync(
            transport,
            WorkspaceAddress(workspace.Id, "settings/network/patchList"),
            workspace.Name,
            cancellationToken);
        return ParseNetworkPatches(reply.Data)
            .FirstOrDefault(patch => patch.Name.Contains(patchName, StringComparison.Ordinal))
            ?? throw new QLabNetworkPatchNotFoundException(patchName);
    }

    private static async Task<string?> QueryStringAsync(
        IQLabOscTransport transport,
        string address,
        string workspaceName,
        CancellationToken cancellationToken)
    {
        using var reply = await SendAsync(transport, address, workspaceName, cancellationToken);
        return GetString(reply.Data);
    }

    private static async Task<QLabOscReply> SendAsync(
        IQLabOscTransport transport,
        string address,
        string workspaceName,
        CancellationToken cancellationToken,
        params object?[] arguments)
    {
        var reply = await transport.SendAsync(new OscMessage(address, arguments), cancellationToken);
        try
        {
            reply.EnsureOk(workspaceName);
            return reply;
        }
        catch
        {
            reply.Dispose();
            throw;
        }
    }

    private static IReadOnlyList<QLabWorkspace> ParseWorkspaces(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<QLabWorkspace>();
        foreach (var item in data.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }
            var id = GetPropertyString(item, "uniqueID", "uniqueId", "id", "workspace_id");
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }
            var name = GetPropertyString(item, "displayName", "name", "workspaceName") ?? id;
            var path = GetPropertyString(item, "path", "filePath", "basePath");
            result.Add(new QLabWorkspace(id, name, path));
        }
        return result;
    }

    private static IReadOnlyList<QLabCueList> ParseCueLists(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<QLabCueList>();
        foreach (var item in data.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }
            var id = GetPropertyString(item, "uniqueID", "uniqueId", "id");
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }
            result.Add(new QLabCueList(
                id,
                GetPropertyString(item, "name", "displayName") ?? string.Empty,
                GetPropertyString(item, "number", "qNumber")));
        }
        return result;
    }

    private static IReadOnlyList<QLabNetworkPatch> ParseNetworkPatches(JsonElement data)
    {
        var result = new List<QLabNetworkPatch>();
        if (data.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in data.EnumerateArray())
            {
                AddPatch(result, item, null);
            }
        }
        else if (data.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in data.EnumerateObject())
            {
                AddPatch(result, property.Value, property.Name);
            }
        }
        return result;
    }

    private static void AddPatch(ICollection<QLabNetworkPatch> result, JsonElement item, string? fallbackId)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            return;
        }
        var id = GetPropertyString(item, "uniqueID", "uniqueId", "id", "patchID") ?? fallbackId;
        var name = GetPropertyString(item, "name", "displayName");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
        {
            return;
        }
        var type = GetPropertyString(item, "type", "patchType", "networkType", "destinationType");
        result.Add(new QLabNetworkPatch(id, name, type));
    }

    private static string? GetPropertyString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
            {
                continue;
            }
            var result = GetString(value);
            if (result is not null)
            {
                return result;
            }
        }
        return null;
    }

    private static string? GetString(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => null
    };

    private static string WorkspaceAddress(string workspaceId, string command) =>
        $"/workspace/{workspaceId}/{command}";

    private static string CueAddress(string workspaceId, string cueId, string property) =>
        $"/workspace/{workspaceId}/cue_id/{cueId}/{property}";
}

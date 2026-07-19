using EosToQLab.Core.Exceptions;
using EosToQLab.Core.Models;
using EosToQLab.Core.Planning;

namespace EosToQLab.Infrastructure.QLab.Workflow;

public sealed class QLabImportWorkflow(
    IQLabOscService oscService,
    IQLabImportPlanBuilder planBuilder,
    QLabImportPlanExecutor planExecutor)
{
    public async Task<QLabImportResult> ExecuteAsync(
        IReadOnlyList<EosCue> cues,
        QLabImportOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cues);
        ArgumentNullException.ThrowIfNull(options);

        await using var session = await oscService.ConnectWorkspaceAsync(
            options.WorkspaceId,
            options.Passcode,
            cancellationToken);
        var workspace = session.Workspace;

        var cueLists = await session.GetCueListsAsync(cancellationToken);
        var conflict = cueLists.FirstOrDefault(list =>
            string.Equals(list.Name, options.CueListName, StringComparison.OrdinalIgnoreCase));
        if (conflict is not null
            && (options.ConflictPolicy != CueListConflictPolicy.ReplaceWithExplicitConsent
                || !options.ExplicitReplacementConsent))
            throw new QLabCueListConflictException(options.CueListName);

        var networkPatches = await session.GetNetworkPatchesAsync(cancellationToken);
        var networkPatch = networkPatches.FirstOrDefault(patch =>
                               string.Equals(patch.Id, options.NetworkPatchId, StringComparison.OrdinalIgnoreCase))
                           ?? throw new QLabNetworkPatchNotFoundException(options.NetworkPatchName);
        if (!QLabProtocol.IsEosNetworkPatchType(networkPatch.Type))
            throw new QLabNetworkPatchTypeMismatchException(networkPatch.Name, networkPatch.Type ?? string.Empty);

        var plan = planBuilder.Build(cues, options);
        var originalCueListId = await session.GetCurrentCueListIdAsync(cancellationToken);

        string? temporaryCueListId = null;
        string? conflictBackupName = null;
        var conflictRenamed = false;
        var conflictDeleted = false;
        var assignedCueNumberCount = 0;
        var preDeletionSaveCompleted = false;

        try
        {
            var temporaryName = $"EosToQLab temporary {Guid.NewGuid():N}";
            temporaryCueListId = await session.CreateCueAsync(
                QLabCueType.CueList,
                temporaryName,
                cancellationToken);
            await session.RenameCueListAsync(
                temporaryCueListId,
                temporaryName,
                temporaryName,
                cancellationToken);
            await session.SetWorkspacePropertyAsync(
                QLabWorkspaceProperty.CurrentCueListId,
                temporaryCueListId,
                cancellationToken);

            var executionResult = await planExecutor.ExecuteAsync(
                session,
                plan,
                new QLabPlanExecutionContext(networkPatch),
                cancellationToken);

            if (conflict is not null)
            {
                conflictBackupName = $"{conflict.Name} (EosToQLab backup {Guid.NewGuid():N})";
                await session.RenameCueListAsync(
                    conflict.Id,
                    conflict.Name,
                    conflictBackupName,
                    cancellationToken);
                conflictRenamed = true;
            }

            await session.RenameCueListAsync(
                temporaryCueListId,
                temporaryName,
                options.CueListName,
                cancellationToken);
            await session.SetWorkspacePropertyAsync(
                QLabWorkspaceProperty.CurrentCueListId,
                temporaryCueListId,
                cancellationToken);

            // Save a recoverable state with both lists before deleting the old one.
            if (conflict is not null && options.SaveWorkspaceAfterImport)
            {
                await session.SaveWorkspaceAsync(cancellationToken);
                preDeletionSaveCompleted = true;
            }

            if (conflict is not null)
            {
                await session.DeleteCueListAsync(
                    conflict.Id,
                    conflictBackupName ?? conflict.Name,
                    cancellationToken);
                conflictDeleted = true;
            }

            assignedCueNumberCount = await QLabImportPlanExecutor.AssignCueNumbersAsync(
                session,
                executionResult.PendingCueNumbers,
                cancellationToken);

            if (options.SaveWorkspaceAfterImport) await session.SaveWorkspaceAsync(cancellationToken);

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
                    for (var index = 0; index < assignedCueNumberCount; index++)
                        await session.UndoAsync(CancellationToken.None);

                    await session.UndoAsync(CancellationToken.None);
                }

                await session.RenameCueListAsync(
                    temporaryCueListId,
                    options.CueListName,
                    $"EosToQLab failed import {Guid.NewGuid():N}",
                    CancellationToken.None);

                if (conflict is not null && conflictRenamed)
                    await session.RenameCueListAsync(
                        conflict.Id,
                        conflictBackupName ?? conflict.Name,
                        conflict.Name,
                        CancellationToken.None);

                if (!string.IsNullOrWhiteSpace(originalCueListId))
                    await session.SetWorkspacePropertyAsync(
                        QLabWorkspaceProperty.CurrentCueListId,
                        originalCueListId,
                        CancellationToken.None);

                await session.DeleteCueListAsync(
                    temporaryCueListId,
                    "failed temporary import",
                    CancellationToken.None);

                if (preDeletionSaveCompleted && options.SaveWorkspaceAfterImport)
                    await session.SaveWorkspaceAsync(CancellationToken.None);
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
}
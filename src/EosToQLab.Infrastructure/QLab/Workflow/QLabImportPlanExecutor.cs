using EosToQLab.Core.Exceptions;
using EosToQLab.Core.Models;

namespace EosToQLab.Infrastructure.QLab.Workflow;

public sealed class QLabImportPlanExecutor
{
    private readonly IReadOnlyDictionary<Type, IQLabPlanItemMapper> _mappers;

    public QLabImportPlanExecutor(IEnumerable<IQLabPlanItemMapper> mappers)
    {
        ArgumentNullException.ThrowIfNull(mappers);

        var mapperList = mappers.ToArray();
        var duplicate = mapperList
            .GroupBy(mapper => mapper.PlanItemType)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException(
                $"Multiple QLab plan-item mappers are registered for {duplicate.Key.Name}.");

        _mappers = mapperList.ToDictionary(mapper => mapper.PlanItemType);
    }

    public async Task<QLabPlanExecutionResult> ExecuteAsync(
        IQLabOscSession session,
        QLabImportPlan plan,
        QLabPlanExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);

        var pendingCueNumbers = new List<QLabPendingCueNumberAssignment>();
        var verifiedNetworkPatchIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in plan.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_mappers.TryGetValue(item.GetType(), out var mapper))
                throw new QLabUnsupportedPlanItemException(item.GetType().Name);

            var request = mapper.Map(item, context);
            var verifyNetworkPatch = request.ExpectedNetworkPatch is not null
                                     && verifiedNetworkPatchIds.Add(request.ExpectedNetworkPatch.Id);
            var pendingCueNumber = await ExecuteCueRequestAsync(
                session,
                request,
                verifyNetworkPatch,
                cancellationToken);
            if (pendingCueNumber is not null) pendingCueNumbers.Add(pendingCueNumber);
        }

        return new QLabPlanExecutionResult(pendingCueNumbers);
    }

    public static async Task<int> AssignCueNumbersAsync(
        IQLabOscSession session,
        IReadOnlyList<QLabPendingCueNumberAssignment> assignments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(assignments);

        var successfulAssignments = 0;
        try
        {
            foreach (var assignment in assignments)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await session.SetCuePropertyAsync(
                        assignment.CueId,
                        QLabCueProperty.Number,
                        assignment.DesiredCueNumber,
                        cancellationToken);
                    successfulAssignments++;
                }
                catch (QLabUnexpectedReplyException)
                {
                    // The cue was cleared before this phase. A rejected EOS number is
                    // therefore left blank; no incremented fallback is ever generated.
                }
            }

            return successfulAssignments;
        }
        catch
        {
            // Keep the workflow rollback boundary directly after cue-list deletion.
            // Undo only number assignments that succeeded before the fatal failure.
            for (var index = 0; index < successfulAssignments; index++) await session.UndoAsync(CancellationToken.None);

            throw;
        }
    }

    private static async Task<QLabPendingCueNumberAssignment?> ExecuteCueRequestAsync(
        IQLabOscSession session,
        QLabCueCreationRequest request,
        bool verifyNetworkPatch,
        CancellationToken cancellationToken)
    {
        try
        {
            var cueId = await session.CreateCueAsync(
                request.CueType,
                request.Name,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.DesiredCueNumber))
                // QLab may automatically number newly created cues. Clear that value first,
                // so a rejected EOS number never leaves an incremented fallback number behind.
                await session.SetCuePropertyAsync(
                    cueId,
                    QLabCueProperty.Number,
                    string.Empty,
                    cancellationToken);

            foreach (var assignment in request.CueProperties)
                await session.SetCuePropertyAsync(
                    cueId,
                    assignment.Property,
                    assignment.Value,
                    cancellationToken);

            if (request.NetworkParameters.Count > 0)
            {
                var orderedParameters = request.NetworkParameters
                    .OrderBy(assignment => assignment.Index)
                    .ToArray();
                for (var index = 0; index < orderedParameters.Length; index++)
                    if (orderedParameters[index].Index != index)
                        throw new InvalidOperationException(
                            "QLab network parameter indices must be contiguous and start at zero.");

                foreach (var assignment in orderedParameters)
                    await session.SetNetworkParameterAsync(
                        cueId,
                        assignment.Index,
                        assignment.Value,
                        cancellationToken);
            }

            if (verifyNetworkPatch && request.ExpectedNetworkPatch is not null)
            {
                var appliedPatchId = await session.QueryCuePropertyAsync(
                    cueId,
                    QLabCueProperty.NetworkPatchId,
                    cancellationToken);
                if (!string.Equals(
                        appliedPatchId,
                        request.ExpectedNetworkPatch.Id,
                        StringComparison.OrdinalIgnoreCase))
                    throw new QLabNetworkPatchAssignmentException(
                        request.Name,
                        request.ExpectedNetworkPatch.Name);
            }

            return string.IsNullOrWhiteSpace(request.DesiredCueNumber)
                ? null
                : new QLabPendingCueNumberAssignment(
                    cueId,
                    request.Name,
                    request.DesiredCueNumber);
        }
        catch (EosToQLabException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new QLabCueCreationException(request.Name, exception);
        }
    }
}
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
        {
            throw new InvalidOperationException(
                $"Multiple QLab plan-item mappers are registered for {duplicate.Key.Name}.");
        }

        _mappers = mapperList.ToDictionary(mapper => mapper.PlanItemType);
    }

    public async Task ExecuteAsync(
        IQLabOscSession session,
        QLabImportPlan plan,
        QLabPlanExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var item in plan.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_mappers.TryGetValue(item.GetType(), out var mapper))
            {
                throw new QLabUnsupportedPlanItemException(item.GetType().Name);
            }

            var request = mapper.Map(item, context);
            await ExecuteCueRequestAsync(session, request, cancellationToken);
        }
    }

    private static async Task ExecuteCueRequestAsync(
        IQLabOscSession session,
        QLabCueCreationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var cueId = await session.CreateCueAsync(
                request.CueType,
                request.Name,
                cancellationToken);

            foreach (var assignment in request.CueProperties)
            {
                await session.SetCuePropertyAsync(
                    cueId,
                    assignment.Property,
                    assignment.Value,
                    cancellationToken);
            }

            foreach (var assignment in request.NetworkParameters)
            {
                await session.SetNetworkParameterAsync(
                    cueId,
                    assignment.Parameter,
                    assignment.Value,
                    cancellationToken);
            }

            if (request.ExpectedNetworkPatch is not null)
            {
                var appliedPatchId = await session.QueryCuePropertyAsync(
                    cueId,
                    QLabCueProperty.NetworkPatchId,
                    cancellationToken);
                if (!string.Equals(
                        appliedPatchId,
                        request.ExpectedNetworkPatch.Id,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new QLabNetworkPatchAssignmentException(
                        request.Name,
                        request.ExpectedNetworkPatch.Name);
                }
            }
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

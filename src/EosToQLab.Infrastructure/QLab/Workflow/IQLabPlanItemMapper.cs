using EosToQLab.Core.Models;

namespace EosToQLab.Infrastructure.QLab.Workflow;

public interface IQLabPlanItemMapper
{
    Type PlanItemType { get; }

    QLabCueCreationRequest Map(
        QLabPlanItem item,
        QLabPlanExecutionContext context);
}

public abstract class QLabPlanItemMapper<TPlanItem> : IQLabPlanItemMapper
    where TPlanItem : QLabPlanItem
{
    public Type PlanItemType => typeof(TPlanItem);

    public QLabCueCreationRequest Map(
        QLabPlanItem item,
        QLabPlanExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(context);

        if (item is not TPlanItem typedItem)
        {
            throw new ArgumentException(
                $"Mapper for {typeof(TPlanItem).Name} cannot map {item.GetType().Name}.",
                nameof(item));
        }

        return Map(typedItem, context);
    }

    protected abstract QLabCueCreationRequest Map(
        TPlanItem item,
        QLabPlanExecutionContext context);
}

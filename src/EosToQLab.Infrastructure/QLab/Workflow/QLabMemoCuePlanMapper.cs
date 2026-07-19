using EosToQLab.Core.Models;

namespace EosToQLab.Infrastructure.QLab.Workflow;

public sealed class QLabMemoCuePlanMapper : QLabPlanItemMapper<QLabMemoCuePlan>
{
    protected override QLabCueCreationRequest Map(
        QLabMemoCuePlan item,
        QLabPlanExecutionContext context)
    {
        var properties = new List<QLabCuePropertyAssignment>
        {
            new(QLabCueProperty.Name, item.Name),
            new(QLabCueProperty.Number, string.Empty),
            new(QLabCueProperty.Armed, item.Armed),
            new(QLabCueProperty.SkipIfDisarmed, item.SkipIfDisarmed)
        };

        if (!string.IsNullOrWhiteSpace(item.Notes))
        {
            properties.Add(new QLabCuePropertyAssignment(QLabCueProperty.Notes, item.Notes));
        }

        return new QLabCueCreationRequest(
            QLabCueType.Memo,
            item.Name,
            properties,
            []);
    }
}

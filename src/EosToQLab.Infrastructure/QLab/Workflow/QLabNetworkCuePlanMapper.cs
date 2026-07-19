using EosToQLab.Core.Models;

namespace EosToQLab.Infrastructure.QLab.Workflow;

public sealed class QLabNetworkCuePlanMapper : QLabPlanItemMapper<QLabNetworkCuePlan>
{
    protected override QLabCueCreationRequest Map(
        QLabNetworkCuePlan item,
        QLabPlanExecutionContext context)
    {
        var properties = new List<QLabCuePropertyAssignment>
        {
            new(QLabCueProperty.Name, item.Name),
            new(QLabCueProperty.NetworkPatchId, context.NetworkPatch.Id)
        };

        if (!string.IsNullOrWhiteSpace(item.Notes))
        {
            properties.Add(new QLabCuePropertyAssignment(QLabCueProperty.Notes, item.Notes));
        }

        return new QLabCueCreationRequest(
            QLabCueType.Network,
            item.Name,
            properties,
            [
                new QLabNetworkParameterAssignment(QLabNetworkParameter.Category, QLabEosNetworkCommand.Category),
                new QLabNetworkParameterAssignment(QLabNetworkParameter.Action, QLabEosNetworkCommand.Action),
                new QLabNetworkParameterAssignment(QLabNetworkParameter.Description, QLabEosNetworkCommand.Description),
                new QLabNetworkParameterAssignment(QLabNetworkParameter.CueListNumber, item.ListNumber),
                new QLabNetworkParameterAssignment(QLabNetworkParameter.CueNumber, item.CueNumber)
            ],
            context.NetworkPatch,
            item.QLabNumber);
    }
}

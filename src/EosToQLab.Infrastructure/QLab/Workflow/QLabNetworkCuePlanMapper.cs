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
            properties.Add(new QLabCuePropertyAssignment(QLabCueProperty.Notes, item.Notes));

        if (!item.Armed)
        {
            properties.Add(new QLabCuePropertyAssignment(QLabCueProperty.Armed, false));
        }

        var eosCommand = QLabEosNetworkCommand.RunCueInSpecificList(
            item.ListNumber,
            item.CueNumber);

        return new QLabCueCreationRequest(
            QLabCueType.Network,
            item.Name,
            properties,
            eosCommand.BuildParameters(),
            context.NetworkPatch,
            item.QLabNumber);
    }
}
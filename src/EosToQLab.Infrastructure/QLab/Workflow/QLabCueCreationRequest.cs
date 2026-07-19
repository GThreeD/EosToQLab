using EosToQLab.Core.Models;

namespace EosToQLab.Infrastructure.QLab.Workflow;

public sealed record QLabCuePropertyAssignment(
    QLabCueProperty Property,
    object? Value);

public sealed record QLabNetworkParameterAssignment(
    QLabNetworkParameter Parameter,
    string Value);

public sealed record QLabCueCreationRequest(
    QLabCueType CueType,
    string Name,
    IReadOnlyList<QLabCuePropertyAssignment> CueProperties,
    IReadOnlyList<QLabNetworkParameterAssignment> NetworkParameters,
    QLabNetworkPatch? ExpectedNetworkPatch = null);

public sealed record QLabPlanExecutionContext(QLabNetworkPatch NetworkPatch);

namespace EosToQLab.Core.Models;

public abstract record QLabPlanItem;

public sealed record QLabNetworkCuePlan(
    string Name,
    string ListNumber,
    string CueNumber,
    string? Notes) : QLabPlanItem;

public sealed record QLabMemoCuePlan(
    string Name,
    string? Notes,
    bool Armed = false,
    bool SkipIfDisarmed = true) : QLabPlanItem;

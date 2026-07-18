namespace EosToQLab.Core.Models;

public sealed record QLabImportPlan(IReadOnlyList<QLabPlanItem> Items)
{
    public int NetworkCueCount => Items.Count(item => item is QLabNetworkCuePlan);
    public int MemoCueCount => Items.Count(item => item is QLabMemoCuePlan);
}

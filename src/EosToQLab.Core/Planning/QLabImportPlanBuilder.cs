using System.Globalization;
using EosToQLab.Core.Diagnostics;
using EosToQLab.Core.Models;

namespace EosToQLab.Core.Planning;

public sealed class QLabImportPlanBuilder : IQLabImportPlanBuilder
{
    public QLabImportPlan Build(
        IReadOnlyList<EosCue> cues,
        QLabImportOptions options,
        ICollection<EosDiagnostic>? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(cues);
        ArgumentNullException.ThrowIfNull(options);

        var items = new List<QLabPlanItem>();
        var triggeredByPreviousCue = new Dictionary<int, bool>();
        string? previousScene = null;

        foreach (var cue in cues.OrderBy(cue => cue.SourceOrder))
        {
            var isFollowedCue = triggeredByPreviousCue.GetValueOrDefault(cue.ListNumber);
            var excludeCue = isFollowedCue
                && options.FollowedCueMode == FollowedCueImportMode.Exclude;

            if (excludeCue)
            {
                diagnostics?.Add(new CueSkippedAfterFollowOrHangWarning(cue.DisplayCueNumber));
            }
            else
            {
                var importDisarmed = isFollowedCue
                    && options.FollowedCueMode == FollowedCueImportMode.ImportDisarmed;

                if (importDisarmed)
                {
                    diagnostics?.Add(new CueImportedDisarmedAfterFollowOrHangWarning(cue.DisplayCueNumber));
                }

                AddSceneMemoIfNeeded(items, cue, options, ref previousScene);
                items.Add(new QLabNetworkCuePlan(
                    NullToEmpty(cue.Label),
                    cue.ListNumber.ToString(CultureInfo.InvariantCulture),
                    cue.CueNumber,
                    cue.CueNumber,
                    NullIfWhiteSpace(cue.CueNotes),
                    Armed: !importDisarmed));
            }

            // This is deliberately updated even when the cue itself is excluded. A chain such as
            // 83.1 -> 83.2 -> 83.3 -> 84 must remain a chain while the plan is being filtered.
            triggeredByPreviousCue[cue.ListNumber] = cue.HasFollowOrHang;
        }

        return new QLabImportPlan(items);
    }

    private static void AddSceneMemoIfNeeded(
        ICollection<QLabPlanItem> items,
        EosCue cue,
        QLabImportOptions options,
        ref string? previousScene)
    {
        if (options.SceneTextMode != SceneTextImportMode.MemoCue
            || string.IsNullOrWhiteSpace(cue.SceneText)
            || string.Equals(previousScene, cue.SceneText, StringComparison.Ordinal))
            return;

        previousScene = cue.SceneText;
        items.Add(new QLabMemoCuePlan(cue.SceneText.Trim(), null));
    }

    private static string NullToEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
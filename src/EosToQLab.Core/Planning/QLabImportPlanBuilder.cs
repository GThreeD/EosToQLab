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
            var excludeFollowedCue = isFollowedCue
                                     && options.FollowedCueMode == FollowedCueImportMode.Exclude;

            if (cue.ImportEnabled && excludeFollowedCue)
            {
                diagnostics?.Add(new CueSkippedAfterFollowOrHangWarning(cue.DisplayCueNumber));
            }
            else if (cue.ImportEnabled)
            {
                var importDisarmed = isFollowedCue
                                     && options.FollowedCueMode == FollowedCueImportMode.ImportDisarmed;

                if (importDisarmed)
                    diagnostics?.Add(new CueImportedDisarmedAfterFollowOrHangWarning(cue.DisplayCueNumber));

                AddSceneMemoIfNeeded(items, cue, options, ref previousScene);
                items.Add(new QLabNetworkCuePlan(
                    NullToEmpty(cue.Label),
                    cue.ListNumber.ToString(CultureInfo.InvariantCulture),
                    cue.CueNumber,
                    cue.CueNumber,
                    NullIfWhiteSpace(cue.CueNotes),
                    !importDisarmed));
            }

            // Follow/hang state is based on the complete EOS sequence, including cues manually
            // deselected in the preview. This keeps later automatic cues classified correctly.
            triggeredByPreviousCue[cue.ListNumber] = cue.HasFollowOrHang;
        }

        return new QLabImportPlan(items);
    }

    private static void AddSceneMemoIfNeeded(
        List<QLabPlanItem> items,
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
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
        var skipNextByList = new Dictionary<int, bool>();
        string? previousScene = null;

        foreach (var cue in cues.OrderBy(cue => cue.SourceOrder))
        {
            var skipCurrent = options.SkipCueAfterFollowOrHang
                && skipNextByList.GetValueOrDefault(cue.ListNumber);

            if (skipCurrent)
            {
                diagnostics?.Add(new CueSkippedAfterFollowOrHangWarning(cue.DisplayCueNumber));
            }
            else
            {
                AddSceneMemoIfNeeded(items, cue, options, ref previousScene);
                items.Add(new QLabNetworkCuePlan(
                    NullToEmpty(cue.Label),
                    cue.ListNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    cue.CueNumber,
                    cue.CueNumber,
                    NullIfWhiteSpace(cue.CueNotes)));
            }

            skipNextByList[cue.ListNumber] = options.SkipCueAfterFollowOrHang && cue.HasFollowOrHang;
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
        {
            return;
        }

        previousScene = cue.SceneText;
        items.Add(new QLabMemoCuePlan(cue.SceneText.Trim(), Notes: null));
    }

    private static string NullToEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

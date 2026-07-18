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
                    BuildCueName(cue, options.CueNamePrefix),
                    cue.ListNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    cue.CueNumber,
                    BuildNotes(cue, options.SceneTextMode)));
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
        if (string.IsNullOrWhiteSpace(cue.SceneText)
            || options.SceneTextMode is SceneTextImportMode.Ignore or SceneTextImportMode.NotesOnly
            || string.Equals(previousScene, cue.SceneText, StringComparison.Ordinal))
        {
            return;
        }

        previousScene = cue.SceneText;
        items.Add(new QLabMemoCuePlan(cue.SceneText.Trim(), $"Scene from EOS cue {cue.DisplayCueNumber}"));
    }

    private static string BuildCueName(EosCue cue, string prefix)
    {
        var suffix = string.IsNullOrWhiteSpace(cue.Label) ? string.Empty : $" - {cue.Label.Trim()}";
        return $"{prefix.Trim()} {cue.DisplayCueNumber}{suffix}";
    }

    private static string? BuildNotes(EosCue cue, SceneTextImportMode mode)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(cue.CueNotes))
        {
            parts.Add(cue.CueNotes.Trim());
        }

        if (mode is SceneTextImportMode.NotesOnly or SceneTextImportMode.MemoCueAndNotes
            && !string.IsNullOrWhiteSpace(cue.SceneText))
        {
            parts.Add($"Scene: {cue.SceneText.Trim()}");
        }

        return parts.Count == 0 ? null : string.Join(Environment.NewLine, parts);
    }
}

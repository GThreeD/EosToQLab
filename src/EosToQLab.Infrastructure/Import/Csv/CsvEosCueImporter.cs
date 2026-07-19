using System.Globalization;
using System.Text;
using EosToQLab.Core.Diagnostics;
using EosToQLab.Core.Exceptions;
using EosToQLab.Core.Import;
using EosToQLab.Core.Models;

namespace EosToQLab.Infrastructure.Import.Csv;

public sealed class CsvEosCueImporter : IEosCueImporter
{
    private static readonly EosCsvObjectBinder<EosCsvCue> Binder = new();

    public EosSourceKind SourceKind => EosSourceKind.Csv;

    public bool CanImport(string fileName)
    {
        return string.Equals(Path.GetExtension(fileName), ".csv", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<EosImportResult> ImportAsync(
        EosImportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string text;
        try
        {
            using var reader = new StreamReader(
                request.Content,
                Encoding.UTF8,
                true,
                16 * 1024,
                true);
            text = await reader.ReadToEndAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or DecoderFallbackException
                                              or ObjectDisposedException)
        {
            throw new CsvReadException(request.FileName, exception);
        }

        var records = CsvRecordReader.Parse(text.TrimStart('\uFEFF'));
        var startIndex = FindMarker(records, "START_TARGETS");
        if (startIndex < 0) throw new CsvStartMarkerMissingException(request.FileName);

        if (startIndex + 1 >= records.Count) throw new CsvHeaderMissingException(request.FileName);

        var columns = CreateColumnMap(records[startIndex + 1]);
        var missing = Binder.FindMissingRequiredColumns(columns);
        if (missing.Count > 0) throw new CsvRequiredColumnMissingException(request.FileName, missing);

        var diagnostics = new List<EosDiagnostic>();
        if (!columns.ContainsKey("FOLLOW")) diagnostics.Add(new CsvFollowColumnMissingWarning());
        if (!columns.ContainsKey("SCENE_TEXT")) diagnostics.Add(new CsvSceneTextColumnMissingWarning());

        var endIndex = FindMarker(records, "END_TARGETS", startIndex + 2);
        if (endIndex < 0)
        {
            endIndex = records.Count;
            diagnostics.Add(new CsvEndMarkerMissingWarning());
        }

        var sourceRows = new List<(EosCsvCue Cue, int RowNumber, int SourceOrder)>();
        var sourceOrder = 0;
        for (var recordIndex = startIndex + 2; recordIndex < endIndex; recordIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rowNumber = recordIndex + 1;
            var csvCue = Binder.Bind(records[recordIndex], columns, rowNumber);
            if (!string.Equals(csvCue.TargetTypeAsText, "Cue", StringComparison.OrdinalIgnoreCase)) continue;

            if (string.IsNullOrWhiteSpace(csvCue.TargetId))
            {
                diagnostics.Add(new CsvCueWithoutTargetIdWarning(rowNumber));
                continue;
            }

            sourceRows.Add((csvCue, rowNumber, sourceOrder++));
        }

        var cues = AggregateAndMap(sourceRows, diagnostics);
        if (cues.Count == 0) diagnostics.Add(new NoCuesFoundWarning());

        return new EosImportResult(
            cues,
            diagnostics,
            SourceKind,
            $"EOS CSV: {request.FileName}");
    }

    private static IReadOnlyList<EosCue> AggregateAndMap(
        IReadOnlyList<(EosCsvCue Cue, int RowNumber, int SourceOrder)> rows,
        ICollection<EosDiagnostic> diagnostics)
    {
        var grouped = rows
            .GroupBy(row => BuildIdentity(row.Cue), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Min(row => row.SourceOrder));

        var result = new List<EosCue>();
        var partCount = 0;
        foreach (var group in grouped)
        {
            var root = group.FirstOrDefault(row => !row.Cue.IsPart);
            if (root.Cue is null) root = group.First();

            partCount += group.Count(row => row.Cue.IsPart);
            var source = root.Cue;
            result.Add(new EosCue
            {
                SourceOrder = result.Count,
                ListNumber = source.TargetListNumber <= 0 ? 1 : source.TargetListNumber,
                CueNumber = NormalizeCueNumber(source.TargetId),
                TargetDcid = NullIfWhiteSpace(source.TargetDcid),
                Label = FirstNonEmpty(group.Select(row => row.Cue.Label)),
                Follow = FirstNonEmpty(group.Select(row => row.Cue.Follow)),
                CueNotes = FirstNonEmpty(group.Select(row => row.Cue.CueNotes)),
                SceneText = FirstNonEmpty(group.Select(row => row.Cue.SceneText)),
                SourceKind = EosSourceKind.Csv,
                AdditionalValues = BuildAdditionalValues(source)
            });
        }

        if (partCount > 0) diagnostics.Add(new CsvCuePartsMergedWarning(partCount));

        return result;
    }

    private static IReadOnlyDictionary<string, string?> BuildAdditionalValues(EosCsvCue cue)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["TIME_DATA"] = cue.TimeData,
            ["UP_DELAY"] = cue.UpDelay,
            ["DOWN_TIME"] = cue.DownTime,
            ["DOWN_DELAY"] = cue.DownDelay,
            ["FOCUS_TIME"] = cue.FocusTime,
            ["FOCUS_DELAY"] = cue.FocusDelay,
            ["COLOR_TIME"] = cue.ColorTime,
            ["COLOR_DELAY"] = cue.ColorDelay,
            ["BEAM_TIME"] = cue.BeamTime,
            ["BEAM_DELAY"] = cue.BeamDelay,
            ["DURATION"] = cue.Duration,
            ["ALERT_TIME"] = cue.AlertTime,
            ["MARK"] = cue.Mark,
            ["BLOCK"] = cue.Block,
            ["ASSERT"] = cue.Assert,
            ["ALL_FADE"] = cue.AllFade,
            ["PREHEAT"] = cue.Preheat,
            ["LINK"] = cue.Link,
            ["LOOP"] = cue.Loop,
            ["CURVE"] = cue.Curve,
            ["RATE"] = cue.Rate,
            ["EXTERNAL_LINKS"] = cue.ExternalLinks,
            ["EFFECTS"] = cue.Effects,
            ["MODE"] = cue.Mode,
            ["SCENE_END"] = cue.SceneEnd,
            ["WIDTH"] = cue.Width,
            ["HEIGHT"] = cue.Height
        };
    }

    private static string BuildIdentity(EosCsvCue cue)
    {
        return !string.IsNullOrWhiteSpace(cue.TargetDcid)
            ? $"{cue.TargetListNumber}|{cue.TargetId}|{cue.TargetDcid}"
            : $"{cue.TargetListNumber}|{cue.TargetId}";
    }

    private static string NormalizeCueNumber(string value)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
            ? number.ToString("0.############################", CultureInfo.InvariantCulture)
            : value.Trim();
    }

    private static string? FirstNonEmpty(IEnumerable<string?> values)
    {
        return values.Select(NullIfWhiteSpace).FirstOrDefault(value => value is not null);
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static IReadOnlyDictionary<string, int> CreateColumnMap(IReadOnlyList<string> header)
    {
        return header.Select((name, index) => (Name: name.Trim(), Index: index))
            .Where(item => item.Name.Length > 0)
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Index, StringComparer.OrdinalIgnoreCase);
    }

    private static int FindMarker(
        IReadOnlyList<IReadOnlyList<string>> records,
        string marker,
        int start = 0)
    {
        for (var index = start; index < records.Count; index++)
            if (records[index].Count > 0
                && string.Equals(records[index][0].Trim().TrimStart('\uFEFF'), marker, StringComparison.Ordinal))
                return index;

        return -1;
    }
}
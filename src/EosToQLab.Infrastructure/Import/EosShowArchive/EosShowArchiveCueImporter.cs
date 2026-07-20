using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using EosToQLab.Core.Diagnostics;
using EosToQLab.Core.Exceptions;
using EosToQLab.Core.Import;
using EosToQLab.Core.Models;

namespace EosToQLab.Infrastructure.Import.EosShowArchive;

public sealed class EosShowArchiveCueImporter : IEosCueImporter
{
    private const byte TextTag = 0x03;
    private const int CueScale = 10_000;
    private static readonly byte[] CueMarker = [0x02, 0x01, 0x00, 0x01, 0x01];
    private static readonly byte[] CueRecordTrailer = [0x00, 0x02, 0x02, 0x02, 0x01, 0x00, 0x00, 0x02];
    private readonly IEosShowArchiveCompatibility _compatibility;

    public EosShowArchiveCueImporter(IEosShowArchiveCompatibility compatibility)
    {
        _compatibility = compatibility ?? throw new ArgumentNullException(nameof(compatibility));
    }

    public EosSourceKind SourceKind => EosSourceKind.ShowArchive;

    public bool CanImport(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return string.Equals(extension, ".esf3d", StringComparison.OrdinalIgnoreCase)
               || string.Equals(extension, ".esf2", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<EosImportResult> ImportAsync(
        EosImportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (data, entryName, archiveFormat, archiveVersion) = await ReadArchiveAsync(request, cancellationToken);
        var recovered = ExtractCues(data, ExtractTextFields(data));
        var diagnostics = new List<EosDiagnostic>
        {
            new EosShowArchiveLossTolerantParsingWarning()
        };

        if (!_compatibility.IsCovered(archiveFormat, archiveVersion))
            diagnostics.Add(new EosShowArchiveVersionNotTestedWarning(archiveFormat, archiveVersion));

        foreach (var cue in recovered.Where(cue => cue.FollowDecodeFailed))
            diagnostics.Add(new EosShowArchiveFollowNotDecodedWarning(FormatCueNumber(cue.RawCueNumber)));

        if (recovered.Count == 0)
        {
            diagnostics.Add(new EosShowArchiveNoCueSequenceWarning());
            diagnostics.Add(new NoCuesFoundWarning());
        }

        var cues = recovered.Select((cue, index) => new EosCue
        {
            SourceOrder = index,
            ListNumber = 1,
            CueNumber = FormatCueNumber(cue.RawCueNumber),
            Label = NullIfWhiteSpace(cue.CueLabel),
            Follow = NullIfWhiteSpace(cue.Follow),
            CueNotes = NullIfWhiteSpace(cue.CueNotes),
            SceneText = NullIfWhiteSpace(cue.SceneLabel),
            SourceKind = EosSourceKind.ShowArchive,
            AdditionalValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["SHOWDAT_OTHER_TEXTS"] = cue.OtherTexts.Count == 0
                    ? null
                    : string.Join(" | ", cue.OtherTexts),
                ["EOS_ARCHIVE_FORMAT"] = archiveFormat,
                ["EOS_ARCHIVE_VERSION"] = archiveVersion
            }
        }).ToArray();

        var archiveType = Path.GetExtension(request.FileName).TrimStart('.').ToUpperInvariant();
        var versionDescription = string.IsNullOrWhiteSpace(archiveVersion)
            ? string.Empty
            : $", EOS {archiveVersion}";

        return new EosImportResult(
            cues,
            diagnostics,
            SourceKind,
            $"{archiveType}: {request.FileName} ({entryName}{versionDescription})");
    }

    private static async Task<(byte[] Data, string EntryName, string? Format, string? Version)> ReadArchiveAsync(
        EosImportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var archive = new ZipArchive(request.Content, ZipArchiveMode.Read, true);
            var entry = FindShallowestEntry(archive, "showdat.dat");
            if (entry is null) throw new ShowDataEntryMissingException(request.FileName);

            var versionEntry = FindShallowestEntry(archive, "version.json");
            var (format, version) = await ReadVersionAsync(versionEntry, cancellationToken);

            await using var input = await entry.OpenAsync(cancellationToken);
            using var buffer = new MemoryStream(entry.Length > int.MaxValue ? 0 : (int)entry.Length);
            await input.CopyToAsync(buffer, cancellationToken);
            return (buffer.ToArray(), entry.FullName, format, version);
        }
        catch (ShowDataEntryMissingException)
        {
            throw;
        }
        catch (InvalidDataException exception)
        {
            throw new EosShowArchiveInvalidException(request.FileName, exception);
        }
        catch (Exception exception) when (exception is IOException or NotSupportedException or ObjectDisposedException)
        {
            throw new ShowDataReadException(request.FileName, exception);
        }
    }


    private static ZipArchiveEntry? FindShallowestEntry(ZipArchive archive, string fileName)
    {
        return archive.Entries
            .Where(candidate => candidate.FullName.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(candidate => candidate.FullName.Count(character => character == '/'))
            .FirstOrDefault();
    }

    private static async Task<(string? Format, string? Version)> ReadVersionAsync(
        ZipArchiveEntry? entry,
        CancellationToken cancellationToken)
    {
        if (entry is null) return (null, null);

        try
        {
            await using var input = await entry.OpenAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(input, cancellationToken: cancellationToken);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return (null, null);

            var format = root.TryGetProperty("Format", out var formatProperty)
                         && formatProperty.ValueKind == JsonValueKind.String
                ? formatProperty.GetString()
                : null;
            var version = root.TryGetProperty("Version", out var versionProperty)
                          && versionProperty.ValueKind == JsonValueKind.String
                ? versionProperty.GetString()
                : null;
            return (NullIfWhiteSpace(format), NullIfWhiteSpace(version));
        }
        catch (Exception exception) when (exception is JsonException or IOException or NotSupportedException)
        {
            return (null, null);
        }
    }

    private static List<TextField> ExtractTextFields(byte[] data, int maxCharacters = 65_535)
    {
        var candidates = new List<TextField>();
        for (var offset = 0; offset <= data.Length - 3; offset++)
        {
            if (data[offset] != TextTag) continue;

            var characterCount = data[offset + 1] | (data[offset + 2] << 8);
            if (characterCount is < 1 or > 65_535 || characterCount > maxCharacters) continue;

            var byteLength = 3 + characterCount * 2;
            var end = offset + byteLength;
            if (end > data.Length) continue;

            string text;
            try
            {
                text = new UnicodeEncoding(false, false, true)
                    .GetString(data, offset + 3, characterCount * 2);
            }
            catch (DecoderFallbackException)
            {
                continue;
            }

            if (PrintableRatio(text) < 0.88) continue;

            candidates.Add(new TextField(offset, byteLength, text, ConfidenceForText(text)));
        }

        var accepted = new List<TextField>();
        var intervals = new List<(int Start, int End)>();
        foreach (var field in candidates
                     .OrderByDescending(candidate => candidate.Confidence == TextConfidence.High)
                     .ThenByDescending(candidate => candidate.ByteLength))
        {
            if (intervals.Any(interval => field.Offset < interval.End && field.End > interval.Start)) continue;

            accepted.Add(field);
            intervals.Add((field.Offset, field.End));
        }

        return accepted.OrderBy(field => field.Offset).ToList();
    }

    private static List<RecoveredCue> ExtractCues(byte[] data, IReadOnlyList<TextField> fields)
    {
        var allCandidates = CueNumberCandidates(data);
        var run = SelectMainCueRun(allCandidates);
        if (run.Count == 0) return [];

        var recovered = new List<RecoveredCue>();
        for (var index = 0; index < run.Count; index++)
        {
            var number = run[index];
            var recordStart = number.MarkerOffset;
            var recordEnd = number.RecordEnd;

            var recordFields = fields
                .Where(field => field.Offset >= recordStart
                                && field.Offset < recordEnd
                                && IsUsefulRecordText(field.Text))
                .ToArray();
            var cueLabel = recordFields.FirstOrDefault(field => field.Offset == number.End);
            var header = EosShowArchiveCueHeaderDecoder.Decode(data, number.End, recordEnd);
            var sceneLabel = recordFields.FirstOrDefault(field =>
                field.Offset > number.End
                && field.Offset > 0
                && data[field.Offset - 1] == 0x02
                && field.Offset != cueLabel?.Offset
                && field.Offset != header.FollowTextOffset);
            var cueNotes = header.CueNotesOffset is { } cueNotesOffset
                ? recordFields.FirstOrDefault(field => field.Offset == cueNotesOffset)
                : !header.Parsed
                    ? recordFields.FirstOrDefault(field =>
                        field.Offset > number.End
                        && field.Offset > 0
                        && data[field.Offset - 1] == 0x04
                        && field.Offset != cueLabel?.Offset
                        && field.Offset != sceneLabel?.Offset
                        && field.Offset != header.FollowTextOffset)
                    : null;

            var excludedOffsets = new HashSet<int>();
            if (cueLabel is not null) excludedOffsets.Add(cueLabel.Offset);
            if (sceneLabel is not null) excludedOffsets.Add(sceneLabel.Offset);
            if (cueNotes is not null) excludedOffsets.Add(cueNotes.Offset);
            if (header.FollowTextOffset is { } followTextOffset) excludedOffsets.Add(followTextOffset);

            recovered.Add(new RecoveredCue(
                number.RawValue,
                cueLabel?.Text.Trim(),
                cueNotes?.Text.Trim(),
                sceneLabel?.Text.Trim(),
                header.Follow,
                header.FollowDecodeFailed,
                recordFields
                    .Where(field => !excludedOffsets.Contains(field.Offset))
                    .Select(field => field.Text.Trim())
                    .ToArray()));
        }

        return recovered;
    }

    private static List<NumberCandidate> CueNumberCandidates(byte[] data)
    {
        var result = new List<NumberCandidate>();
        var position = 0;
        while (position <= data.Length - CueMarker.Length)
        {
            var markerOffset = FindPattern(data, CueMarker, position);
            if (markerOffset < 0) break;

            var numberOffset = markerOffset + CueMarker.Length;
            if (TryDecodeTaggedUnsigned(data, numberOffset, out var rawValue, out var end)
                && rawValue is >= CueScale and <= 99_999_999)
            {
                var nextMarkerOffset = FindPattern(data, CueMarker, markerOffset + 1);
                var recordBoundary = nextMarkerOffset >= 0 ? nextMarkerOffset : data.Length;
                // The canonical trailer is a strong cue-list anchor, but real EOS records
                // may contain additional payload before the next cue marker. In that case
                // the fully decoded cue header is sufficient to keep the record candidate.
                var hasCanonicalTrailer =
                    TryFindCueRecordEnd(data, end, recordBoundary, out _);
                var header = EosShowArchiveCueHeaderDecoder.Decode(data, end, recordBoundary);
                if (hasCanonicalTrailer || header.Parsed)
                    result.Add(new NumberCandidate(
                        markerOffset,
                        numberOffset,
                        rawValue,
                        end,
                        recordBoundary,
                        hasCanonicalTrailer));
            }

            position = markerOffset + 1;
        }

        return result;
    }

    private static List<NumberCandidate> SelectMainCueRun(List<NumberCandidate> candidates)
    {
        // Rank structurally adjacent runs by their canonical cue-record anchors. This keeps
        // header-valid record variants in the cue list without selecting EOS effect records
        // that reuse cue-like numbers but do not contain canonical cue anchors.
        return SplitMonotonicRuns(candidates)
            .Where(run => run.All(item => item.RawValue % 10 == 0))
            .Where(run => run.Any(item => item.HasCanonicalTrailer))
            .OrderByDescending(run => run.Count(item => item.HasCanonicalTrailer))
            .ThenByDescending(run => run.Count)
            .ThenByDescending(run => run[^1].MarkerOffset - run[0].MarkerOffset)
            .FirstOrDefault() ?? [];
    }

    private static bool TryFindCueRecordEnd(
        byte[] data,
        int start,
        int boundary,
        out int recordEnd)
    {
        recordEnd = 0;
        var position = start;
        while (position < boundary)
        {
            var trailerOffset = FindPattern(data, CueRecordTrailer, position);
            if (trailerOffset < 0 || trailerOffset + CueRecordTrailer.Length >= boundary) return false;

            var sceneOffset = trailerOffset + CueRecordTrailer.Length;
            var sceneEnd = SceneValueEnd(data, sceneOffset, boundary);
            if (sceneEnd >= 0
                && sceneEnd + 3 == boundary
                && data[sceneEnd] == 0x04
                && data[sceneEnd + 1] == 0x04
                && data[sceneEnd + 2] == 0x04)
            {
                recordEnd = boundary;
                return true;
            }

            position = trailerOffset + 1;
        }

        return false;
    }

    private static int SceneValueEnd(byte[] data, int offset, int boundary)
    {
        if (offset >= boundary) return -1;
        if (data[offset] == 0x00) return offset + 1;
        if (data[offset] != TextTag || offset + 3 > boundary) return -1;

        var characterCount = data[offset + 1] | (data[offset + 2] << 8);
        var end = offset + 3 + characterCount * 2;
        return end <= boundary ? end : -1;
    }

    private static IEnumerable<List<NumberCandidate>> SplitMonotonicRuns(
        IReadOnlyList<NumberCandidate> candidates)
    {
        if (candidates.Count == 0) yield break;

        var current = new List<NumberCandidate> { candidates[0] };
        for (var index = 1; index < candidates.Count; index++)
        {
            var candidate = candidates[index];
            var previous = current[^1];
            var offsetGap = candidate.MarkerOffset - previous.MarkerOffset;
            var structurallyAdjacent = previous.RecordEnd == candidate.MarkerOffset;
            if (structurallyAdjacent
                && candidate.RawValue > previous.RawValue
                && offsetGap is > 0 and <= 131_072)
            {
                current.Add(candidate);
            }
            else
            {
                yield return current;
                current = [candidate];
            }
        }

        yield return current;
    }

    private static bool TryDecodeTaggedUnsigned(byte[] data, int offset, out int value, out int end)
    {
        value = 0;
        end = offset;
        if (offset >= data.Length) return false;

        var width = data[offset] switch
        {
            0x08 => 1,
            0x09 => 2,
            0x0A => 3,
            _ => 0
        };
        if (width == 0 || offset + 1 + width > data.Length) return false;

        for (var index = 0; index < width; index++) value |= data[offset + 1 + index] << (8 * index);

        end = offset + 1 + width;
        return true;
    }

    private static int FindPattern(byte[] data, byte[] pattern, int start)
    {
        for (var offset = start; offset <= data.Length - pattern.Length; offset++)
        {
            var matches = true;
            for (var index = 0; index < pattern.Length; index++)
            {
                if (data[offset + index] == pattern[index]) continue;

                matches = false;
                break;
            }

            if (matches) return offset;
        }

        return -1;
    }

    private static bool IsUsefulRecordText(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length is 0 or > 120 || Guid.TryParse(trimmed.TrimStart('$'), out _)) return false;

        return trimmed.Length != 1 || char.IsLetterOrDigit(trimmed[0]);
    }

    private static double PrintableRatio(string text)
    {
        var runes = text.EnumerateRunes().ToArray();
        if (runes.Length == 0) return 0;

        var good = runes.Count(rune =>
        {
            var category = Rune.GetUnicodeCategory(rune);
            return rune.Value is '\t' or '\r' or '\n'
                   || category is UnicodeCategory.UppercaseLetter
                       or UnicodeCategory.LowercaseLetter
                       or UnicodeCategory.TitlecaseLetter
                       or UnicodeCategory.ModifierLetter
                       or UnicodeCategory.OtherLetter
                       or UnicodeCategory.DecimalDigitNumber
                       or UnicodeCategory.LetterNumber
                       or UnicodeCategory.OtherNumber
                       or UnicodeCategory.ConnectorPunctuation
                       or UnicodeCategory.DashPunctuation
                       or UnicodeCategory.OpenPunctuation
                       or UnicodeCategory.ClosePunctuation
                       or UnicodeCategory.InitialQuotePunctuation
                       or UnicodeCategory.FinalQuotePunctuation
                       or UnicodeCategory.OtherPunctuation
                       or UnicodeCategory.MathSymbol
                       or UnicodeCategory.CurrencySymbol
                       or UnicodeCategory.ModifierSymbol
                       or UnicodeCategory.OtherSymbol
                       or UnicodeCategory.SpaceSeparator
                       or UnicodeCategory.LineSeparator
                       or UnicodeCategory.ParagraphSeparator;
        });

        return (double)good / runes.Length;
    }

    private static TextConfidence ConfidenceForText(string text)
    {
        var runes = text.EnumerateRunes().ToArray();
        var asciiCount = runes.Count(rune => rune.Value is '\t' or '\r' or '\n' || rune.Value is >= 0x20 and <= 0x7E);
        var asciiRatio = (double)asciiCount / Math.Max(runes.Length, 1);
        var printable = PrintableRatio(text);
        if (printable >= 0.98 && asciiRatio >= 0.90) return TextConfidence.High;

        return printable >= 0.95 ? TextConfidence.Medium : TextConfidence.Low;
    }

    private static string FormatCueNumber(int rawValue)
    {
        return ((decimal)rawValue / CueScale).ToString("0.####", CultureInfo.InvariantCulture);
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private enum TextConfidence
    {
        Low,
        Medium,
        High
    }

    private sealed record TextField(int Offset, int ByteLength, string Text, TextConfidence Confidence)
    {
        public int End => Offset + ByteLength;
    }

    private sealed record NumberCandidate(
        int MarkerOffset,
        int Offset,
        int RawValue,
        int End,
        int RecordEnd,
        bool HasCanonicalTrailer);

    private sealed record RecoveredCue(
        int RawCueNumber,
        string? CueLabel,
        string? CueNotes,
        string? SceneLabel,
        string? Follow,
        bool FollowDecodeFailed,
        IReadOnlyList<string> OtherTexts);
}

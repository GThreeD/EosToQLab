using System.Globalization;
using System.IO.Compression;
using System.Text;
using EosToQLab.Core.Diagnostics;
using EosToQLab.Core.Exceptions;
using EosToQLab.Core.Import;
using EosToQLab.Core.Models;

namespace EosToQLab.Infrastructure.Import.Esf3d;

public sealed class Esf3dEosCueImporter : IEosCueImporter
{
    private const byte TextTag = 0x03;
    private const int CueScale = 10_000;
    private static readonly byte[] CueMarker = [0x02, 0x01, 0x00, 0x01, 0x01];

    public EosSourceKind SourceKind => EosSourceKind.Esf3d;

    public bool CanImport(string fileName) =>
        string.Equals(Path.GetExtension(fileName), ".esf3d", StringComparison.OrdinalIgnoreCase);

    public async Task<EosImportResult> ImportAsync(
        EosImportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (data, entryName) = await ReadShowDataAsync(request, cancellationToken);
        var recovered = ExtractCues(data, ExtractTextFields(data));
        var diagnostics = new List<EosDiagnostic>
        {
            new Esf3dLossTolerantParsingWarning(),
            new Esf3dFollowNotDecodedWarning()
        };

        if (recovered.Count == 0)
        {
            diagnostics.Add(new Esf3dNoCueSequenceWarning());
            diagnostics.Add(new NoCuesFoundWarning());
        }

        var cues = recovered.Select((cue, index) => new EosCue
        {
            SourceOrder = index,
            ListNumber = 1,
            CueNumber = FormatCueNumber(cue.RawCueNumber),
            Label = NullIfWhiteSpace(cue.CueLabel),
            SceneText = NullIfWhiteSpace(cue.SceneLabel),
            SourceKind = EosSourceKind.Esf3d,
            AdditionalValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["SHOWDAT_OTHER_TEXTS"] = cue.OtherTexts.Count == 0
                    ? null
                    : string.Join(" | ", cue.OtherTexts)
            }
        }).ToArray();

        return new EosImportResult(
            cues,
            diagnostics,
            SourceKind,
            $"ESF3D: {request.FileName} ({entryName})");
    }

    private static async Task<(byte[] Data, string EntryName)> ReadShowDataAsync(
        EosImportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var archive = new ZipArchive(request.Content, ZipArchiveMode.Read, leaveOpen: true);
            var entry = archive.Entries
                .Where(candidate => candidate.FullName.EndsWith("showdat.dat", StringComparison.OrdinalIgnoreCase))
                .OrderBy(candidate => candidate.FullName.Count(character => character == '/'))
                .FirstOrDefault();

            if (entry is null)
            {
                throw new ShowDataEntryMissingException(request.FileName);
            }

            await using var input = entry.Open();
            using var buffer = new MemoryStream(entry.Length > int.MaxValue ? 0 : (int)entry.Length);
            await input.CopyToAsync(buffer, cancellationToken);
            return (buffer.ToArray(), entry.FullName);
        }
        catch (ShowDataEntryMissingException)
        {
            throw;
        }
        catch (InvalidDataException exception)
        {
            throw new Esf3dArchiveInvalidException(request.FileName, exception);
        }
        catch (Exception exception) when (exception is IOException or NotSupportedException or ObjectDisposedException)
        {
            throw new ShowDataReadException(request.FileName, exception);
        }
    }

    private static IReadOnlyList<TextField> ExtractTextFields(byte[] data, int maxCharacters = 65_535)
    {
        var candidates = new List<TextField>();
        for (var offset = 0; offset <= data.Length - 3; offset++)
        {
            if (data[offset] != TextTag)
            {
                continue;
            }

            var characterCount = data[offset + 1] | (data[offset + 2] << 8);
            if (characterCount is < 1 or > 65_535 || characterCount > maxCharacters)
            {
                continue;
            }

            var byteLength = 3 + characterCount * 2;
            var end = offset + byteLength;
            if (end > data.Length)
            {
                continue;
            }

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

            if (PrintableRatio(text) < 0.88)
            {
                continue;
            }

            candidates.Add(new TextField(offset, byteLength, text, ConfidenceForText(text)));
        }

        var accepted = new List<TextField>();
        var intervals = new List<(int Start, int End)>();
        foreach (var field in candidates
                     .OrderByDescending(candidate => candidate.Confidence == TextConfidence.High)
                     .ThenByDescending(candidate => candidate.ByteLength))
        {
            if (intervals.Any(interval => field.Offset < interval.End && field.End > interval.Start))
            {
                continue;
            }

            accepted.Add(field);
            intervals.Add((field.Offset, field.End));
        }

        return accepted.OrderBy(field => field.Offset).ToArray();
    }

    private static IReadOnlyList<RecoveredCue> ExtractCues(byte[] data, IReadOnlyList<TextField> fields)
    {
        var allCandidates = CueNumberCandidates(data);
        var run = SelectMainCueRun(allCandidates);
        if (run.Count == 0)
        {
            return [];
        }

        var recovered = new List<RecoveredCue>();
        for (var index = 0; index < run.Count; index++)
        {
            var number = run[index];
            var recordStart = number.Offset - CueMarker.Length;
            int recordEnd;
            if (index + 1 < run.Count)
            {
                recordEnd = run[index + 1].Offset - CueMarker.Length;
            }
            else
            {
                var globalIndex = allCandidates.FindIndex(candidate => candidate.Offset == number.Offset);
                recordEnd = globalIndex >= 0 && globalIndex + 1 < allCandidates.Count
                    ? allCandidates[globalIndex + 1].Offset - CueMarker.Length
                    : data.Length;
            }

            var recordFields = fields
                .Where(field => field.Offset >= recordStart
                    && field.Offset < recordEnd
                    && IsUsefulRecordText(field.Text))
                .ToArray();
            var cueLabel = recordFields.FirstOrDefault(field => field.Offset == number.End);
            var sceneLabel = recordFields.FirstOrDefault(field =>
                field.Offset > number.End
                && field.Offset > 0
                && data[field.Offset - 1] == 0x02
                && field.Offset != cueLabel?.Offset);

            var excludedOffsets = new HashSet<int>();
            if (cueLabel is not null)
            {
                excludedOffsets.Add(cueLabel.Offset);
            }
            if (sceneLabel is not null)
            {
                excludedOffsets.Add(sceneLabel.Offset);
            }

            recovered.Add(new RecoveredCue(
                number.RawValue,
                cueLabel?.Text.Trim(),
                sceneLabel?.Text.Trim(),
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
            if (markerOffset < 0)
            {
                break;
            }

            var numberOffset = markerOffset + CueMarker.Length;
            if (TryDecodeTaggedUnsigned(data, numberOffset, out var rawValue, out var end)
                && rawValue is >= CueScale and <= 99_999_999)
            {
                result.Add(new NumberCandidate(numberOffset, rawValue, end));
            }

            position = markerOffset + 1;
        }

        return result;
    }

    private static List<NumberCandidate> SelectMainCueRun(List<NumberCandidate> candidates) =>
        SplitMonotonicRuns(candidates)
            .Where(run => run.Count >= 2
                && run[0].RawValue <= 100 * CueScale
                && run.All(item => item.RawValue % 10 == 0))
            .OrderByDescending(run => run.Count)
            .ThenByDescending(run => run[^1].Offset - run[0].Offset)
            .FirstOrDefault() ?? [];

    private static IEnumerable<List<NumberCandidate>> SplitMonotonicRuns(
        IReadOnlyList<NumberCandidate> candidates)
    {
        if (candidates.Count == 0)
        {
            yield break;
        }

        var current = new List<NumberCandidate> { candidates[0] };
        for (var index = 1; index < candidates.Count; index++)
        {
            var candidate = candidates[index];
            var previous = current[^1];
            var offsetGap = candidate.Offset - previous.Offset;
            if (candidate.RawValue > previous.RawValue && offsetGap is > 0 and <= 131_072)
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
        if (offset >= data.Length)
        {
            return false;
        }

        var width = data[offset] switch
        {
            0x08 => 1,
            0x09 => 2,
            0x0A => 3,
            _ => 0
        };
        if (width == 0 || offset + 1 + width > data.Length)
        {
            return false;
        }

        for (var index = 0; index < width; index++)
        {
            value |= data[offset + 1 + index] << (8 * index);
        }

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
                if (data[offset + index] == pattern[index])
                {
                    continue;
                }

                matches = false;
                break;
            }

            if (matches)
            {
                return offset;
            }
        }

        return -1;
    }

    private static bool IsUsefulRecordText(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length is 0 or > 120 || Guid.TryParse(trimmed.TrimStart('$'), out _))
        {
            return false;
        }

        return trimmed.Length != 1 || char.IsLetterOrDigit(trimmed[0]);
    }

    private static double PrintableRatio(string text)
    {
        var runes = text.EnumerateRunes().ToArray();
        if (runes.Length == 0)
        {
            return 0;
        }

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
        if (printable >= 0.98 && asciiRatio >= 0.90)
        {
            return TextConfidence.High;
        }

        return printable >= 0.95 ? TextConfidence.Medium : TextConfidence.Low;
    }

    private static string FormatCueNumber(int rawValue) =>
        ((decimal)rawValue / CueScale).ToString("0.####", CultureInfo.InvariantCulture);

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private enum TextConfidence { Low, Medium, High }
    private sealed record TextField(int Offset, int ByteLength, string Text, TextConfidence Confidence)
    {
        public int End => Offset + ByteLength;
    }
    private sealed record NumberCandidate(int Offset, int RawValue, int End);
    private sealed record RecoveredCue(
        int RawCueNumber,
        string? CueLabel,
        string? SceneLabel,
        IReadOnlyList<string> OtherTexts);
}

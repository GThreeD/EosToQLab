using System.Text.Json;

namespace EosToQLab.Tests.TestDoubles;

internal static class EosShowArchiveContractAssertions
{
    public static async Task<EosImportResult> ImportAndMatchAsync(
        EosShowArchiveCueImporter importer,
        string archivePath,
        string expectedPath)
    {
        await using var stream = File.OpenRead(archivePath);
        using var expected = JsonDocument.Parse(
            await File.ReadAllTextAsync(expectedPath, TestContext.Current.CancellationToken));
        var root = expected.RootElement;
        var strict = !root.TryGetProperty("strict", out var strictProperty) || strictProperty.GetBoolean();
        var expectedCues = root.GetProperty("cues").EnumerateArray().ToArray();

        var result = await importer.ImportAsync(
            new EosImportRequest(Path.GetFileName(archivePath), stream),
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedCues.Length, result.Cues.Count);
        for (var index = 0; index < expectedCues.Length; index++)
        {
            var contract = expectedCues[index];
            var actual = result.Cues[index];

            Assert.Equal(contract.GetProperty("sourceOrder").GetInt32(), actual.SourceOrder);
            Assert.Equal(contract.GetProperty("listNumber").GetInt32(), actual.ListNumber);
            Assert.Equal(contract.GetProperty("cueNumber").GetString(), actual.CueNumber);
            AssertOptional(contract, "label", actual.Label, strict);
            AssertOptional(contract, "notes", actual.CueNotes, strict);
            AssertOptional(contract, "scene", actual.SceneText, strict);
            AssertOptional(contract, "follow", actual.Follow, strict);
            Assert.Equal(EosSourceKind.ShowArchive, actual.SourceKind);
        }

        if (root.TryGetProperty("archive", out var archiveContract))
        {
            var format = archiveContract.GetProperty("format").GetString();
            var version = archiveContract.GetProperty("version").GetString();
            Assert.All(result.Cues, cue =>
            {
                Assert.Equal(format, cue.AdditionalValues["EOS_ARCHIVE_FORMAT"]);
                Assert.Equal(version, cue.AdditionalValues["EOS_ARCHIVE_VERSION"]);
            });
        }

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic is EosShowArchiveLossTolerantParsingWarning);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic is EosShowArchiveFollowNotDecodedWarning);
        Assert.Contains("showdat.dat", result.SourceDescription, StringComparison.OrdinalIgnoreCase);
        return result;
    }

    private static void AssertOptional(
        JsonElement contract,
        string propertyName,
        string? actual,
        bool strict)
    {
        if (contract.TryGetProperty(propertyName, out var expected))
        {
            Assert.Equal(expected.GetString(), actual);
            return;
        }

        if (strict) Assert.Null(actual);
    }
}
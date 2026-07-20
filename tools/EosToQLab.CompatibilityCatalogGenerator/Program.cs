using System.IO.Compression;
using System.Text.Json;

if (args.Length != 2)
    throw new ArgumentException(
        "Usage: EosToQLab.CompatibilityCatalogGenerator <compatibility-fixture-root> <output-json>");

var fixtureRoot = Path.GetFullPath(args[0]);
var outputPath = Path.GetFullPath(args[1]);
if (!Directory.Exists(fixtureRoot))
    throw new DirectoryNotFoundException(
        $"EOS show archive compatibility fixture root was not found: {fixtureRoot}");

var fixtureDirectories = Directory.EnumerateFiles(fixtureRoot, "*", SearchOption.AllDirectories)
    .Where(path => IsShowArchiveExtension(Path.GetExtension(path))
                   || string.Equals(Path.GetFileName(path), "expected.json", StringComparison.Ordinal))
    .Select(path => Path.GetDirectoryName(path)!)
    .Distinct(StringComparer.Ordinal)
    .OrderBy(path => path, StringComparer.Ordinal)
    .ToArray();
if (fixtureDirectories.Length == 0)
    throw new InvalidDataException(
        $"No EOS show archive compatibility fixtures were found in '{fixtureRoot}'.");

var entries = new List<(string Format, string Version, string Fixture)>();
foreach (var fixtureDirectory in fixtureDirectories)
{
    var fixtureName = Path.GetRelativePath(fixtureRoot, fixtureDirectory)
        .Replace(Path.DirectorySeparatorChar, '/');
    var contracts = Directory.EnumerateFiles(fixtureDirectory, "expected.json", SearchOption.TopDirectoryOnly)
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToArray();
    var archives = Directory.EnumerateFiles(fixtureDirectory, "*", SearchOption.TopDirectoryOnly)
        .Where(path => IsShowArchiveExtension(Path.GetExtension(path)))
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToArray();
    if (contracts.Length != 1 || archives.Length != 1)
        throw new InvalidDataException(
            $"Compatibility fixture '{fixtureName}' must contain exactly one .esf2 or .esf3d archive and one expected.json contract.");

    var expectedIdentity = ReadExpectedIdentity(contracts[0], fixtureName);
    var archiveIdentity = ReadArchiveIdentity(archives[0], fixtureName);
    if (!IdentityEquals(expectedIdentity, archiveIdentity))
        throw new InvalidDataException(
            $"Compatibility fixture '{fixtureName}' declares " +
            $"'{expectedIdentity.Format}'/'{expectedIdentity.Version}' in expected.json, " +
            $"but version.json contains '{archiveIdentity.Format}'/'{archiveIdentity.Version}'.");

    entries.Add((
        archiveIdentity.Format,
        archiveIdentity.Version,
        fixtureName));
}

var duplicate = entries
    .GroupBy(
        entry => $"{entry.Format.Trim()}\u001f{entry.Version.Trim()}",
        StringComparer.OrdinalIgnoreCase)
    .FirstOrDefault(group => group.Count() > 1);
if (duplicate is not null)
    throw new InvalidDataException(
        "More than one compatibility fixture covers the same EOS archive identity: " +
        string.Join(", ", duplicate.Select(entry => entry.Fixture)));

var catalog = new
{
    schemaVersion = 1,
    archives = entries
        .OrderBy(entry => entry.Format, StringComparer.OrdinalIgnoreCase)
        .ThenBy(entry => entry.Version, StringComparer.OrdinalIgnoreCase)
        .Select(entry => new
        {
            format = entry.Format,
            version = entry.Version,
            fixture = entry.Fixture
        })
        .ToArray()
};
var json = JsonSerializer.Serialize(catalog, new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
}) + "\n";

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
if (!File.Exists(outputPath) || !string.Equals(File.ReadAllText(outputPath), json, StringComparison.Ordinal))
{
    var temporaryPath = outputPath + ".tmp";
    File.WriteAllText(temporaryPath, json);
    File.Move(temporaryPath, outputPath, true);
}

static (string Format, string Version) ReadExpectedIdentity(string expectedPath, string fixtureName)
{
    using var document = JsonDocument.Parse(File.ReadAllText(expectedPath));
    var root = document.RootElement;
    if (root.ValueKind != JsonValueKind.Object
        || !root.TryGetProperty("archive", out var archive)
        || archive.ValueKind != JsonValueKind.Object)
        throw new InvalidDataException(
            $"Compatibility fixture '{fixtureName}' must declare archive.format and archive.version in expected.json.");

    return ReadIdentity(archive, "format", "version", $"expected.json of '{fixtureName}'");
}

static (string Format, string Version) ReadArchiveIdentity(string archivePath, string fixtureName)
{
    using var archive = ZipFile.OpenRead(archivePath);
    _ = archive.Entries
            .Where(entry => entry.FullName.EndsWith("showdat.dat", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.FullName.Count(character => character == '/'))
            .FirstOrDefault()
        ?? throw new InvalidDataException(
            $"Compatibility fixture '{fixtureName}' has no showdat.dat payload.");

    var manifest = archive.Entries
                       .Where(entry => entry.FullName.EndsWith("version.json", StringComparison.OrdinalIgnoreCase))
                       .OrderBy(entry => entry.FullName.Count(character => character == '/'))
                       .FirstOrDefault()
                   ?? throw new InvalidDataException(
                       $"Compatibility fixture '{fixtureName}' has no version.json manifest.");

    using var stream = manifest.Open();
    using var document = JsonDocument.Parse(stream);
    return ReadIdentity(document.RootElement, "Format", "Version", $"version.json of '{fixtureName}'");
}

static (string Format, string Version) ReadIdentity(
    JsonElement element,
    string formatPropertyName,
    string versionPropertyName,
    string source)
{
    if (element.ValueKind != JsonValueKind.Object
        || !element.TryGetProperty(formatPropertyName, out var formatProperty)
        || formatProperty.ValueKind != JsonValueKind.String
        || !element.TryGetProperty(versionPropertyName, out var versionProperty)
        || versionProperty.ValueKind != JsonValueKind.String)
        throw new InvalidDataException($"{source} has no readable format/version identity.");

    var format = formatProperty.GetString()?.Trim();
    var version = versionProperty.GetString()?.Trim();
    if (string.IsNullOrWhiteSpace(format) || string.IsNullOrWhiteSpace(version))
        throw new InvalidDataException($"{source} has an empty format/version identity.");

    return (format, version);
}

static bool IdentityEquals(
    (string Format, string Version) left,
    (string Format, string Version) right)
{
    return StringComparer.OrdinalIgnoreCase.Equals(left.Format, right.Format)
           && StringComparer.OrdinalIgnoreCase.Equals(left.Version, right.Version);
}

static bool IsShowArchiveExtension(string extension)
{
    return string.Equals(extension, ".esf2", StringComparison.OrdinalIgnoreCase)
           || string.Equals(extension, ".esf3d", StringComparison.OrdinalIgnoreCase);
}
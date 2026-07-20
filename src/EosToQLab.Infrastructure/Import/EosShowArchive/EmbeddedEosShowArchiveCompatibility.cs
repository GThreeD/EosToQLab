using System.Reflection;
using System.Text.Json;
using EosToQLab.Core.Import;

namespace EosToQLab.Infrastructure.Import.EosShowArchive;

public sealed class EmbeddedEosShowArchiveCompatibility : IEosShowArchiveCompatibility
{
    internal const string ResourceName =
        "EosToQLab.Infrastructure.Import.EosShowArchive.tested-archives.json";

    private readonly HashSet<ArchiveIdentity> _coveredArchives;

    public EmbeddedEosShowArchiveCompatibility()
        : this(typeof(EmbeddedEosShowArchiveCompatibility).Assembly)
    {
    }

    internal EmbeddedEosShowArchiveCompatibility(Assembly assembly)
        : this(OpenEmbeddedCatalog(assembly))
    {
    }

    internal EmbeddedEosShowArchiveCompatibility(Stream catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        using (catalog)
        using (var document = JsonDocument.Parse(catalog))
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("schemaVersion", out var schemaVersion)
                || schemaVersion.ValueKind != JsonValueKind.Number
                || schemaVersion.GetInt32() != 1
                || !root.TryGetProperty("archives", out var archives)
                || archives.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException(
                    "The embedded EOS show archive compatibility catalog has an unsupported schema.");

            _coveredArchives = archives
                .EnumerateArray()
                .Select(ParseIdentity)
                .ToHashSet(ArchiveIdentityComparer.Instance);
        }
    }

    public bool IsCovered(string? format, string? version)
    {
        if (string.IsNullOrWhiteSpace(format) || string.IsNullOrWhiteSpace(version)) return false;

        return _coveredArchives.Contains(new ArchiveIdentity(format.Trim(), version.Trim()));
    }

    private static Stream OpenEmbeddedCatalog(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return assembly.GetManifestResourceStream(ResourceName)
               ?? throw new InvalidOperationException(
                   $"The embedded EOS show archive compatibility catalog '{ResourceName}' is missing.");
    }

    private static ArchiveIdentity ParseIdentity(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty("format", out var formatProperty)
            || formatProperty.ValueKind != JsonValueKind.String
            || !element.TryGetProperty("version", out var versionProperty)
            || versionProperty.ValueKind != JsonValueKind.String)
            throw new InvalidDataException(
                "The embedded EOS show archive compatibility catalog contains an invalid archive entry.");

        var format = formatProperty.GetString()?.Trim();
        var version = versionProperty.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(format) || string.IsNullOrWhiteSpace(version))
            throw new InvalidDataException(
                "The embedded EOS show archive compatibility catalog contains an empty format or version.");

        return new ArchiveIdentity(format, version);
    }

    private sealed record ArchiveIdentity(string Format, string Version);

    private sealed class ArchiveIdentityComparer : IEqualityComparer<ArchiveIdentity>
    {
        public static ArchiveIdentityComparer Instance { get; } = new();

        public bool Equals(ArchiveIdentity? x, ArchiveIdentity? y)
        {
            return ReferenceEquals(x, y)
                   || (x is not null
                       && y is not null
                       && StringComparer.OrdinalIgnoreCase.Equals(x.Format, y.Format)
                       && StringComparer.OrdinalIgnoreCase.Equals(x.Version, y.Version));
        }

        public int GetHashCode(ArchiveIdentity obj)
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Format),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Version));
        }
    }
}
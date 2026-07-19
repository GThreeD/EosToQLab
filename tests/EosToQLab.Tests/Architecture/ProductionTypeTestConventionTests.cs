using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace EosToQLab.Tests.Architecture;

public sealed class ProductionTypeTestConventionTests
{
    [Fact]
    public void Every_top_level_Core_and_Infrastructure_type_has_a_dedicated_test_class()
    {
        var productionTypes = new[]
            {
                typeof(EosCue).Assembly,
                typeof(CsvEosCueImporter).Assembly
            }
            .Distinct()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => !type.IsNested)
            .Where(type => !type.Name.StartsWith('<'))
            .Where(type => !type.IsDefined(typeof(CompilerGeneratedAttribute), false))
            .Where(type => type.Namespace?.StartsWith("EosToQLab.", StringComparison.Ordinal) == true)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        var testTypeNames = typeof(ProductionTypeTestConventionTests).Assembly
            .GetTypes()
            .Where(type => type.Name.EndsWith("Tests", StringComparison.Ordinal))
            .Select(type => type.Name)
            .ToHashSet(StringComparer.Ordinal);

        var missing = productionTypes
            .Where(type => !testTypeNames.Contains($"{RemoveGenericArity(type.Name)}Tests"))
            .Select(type => type.FullName)
            .ToArray();

        Assert.True(
            missing.Length == 0,
            $"Production types without a dedicated test class:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    [Fact]
    public void Every_top_level_Core_and_Infrastructure_type_has_its_own_test_file()
    {
        var repositoryRoot = FindRepositoryRoot();
        var productionRoots = new[]
        {
            Path.Combine(repositoryRoot, "src", "EosToQLab.Core"),
            Path.Combine(repositoryRoot, "src", "EosToQLab.Infrastructure")
        };
        var testRoot = Path.Combine(repositoryRoot, "tests", "EosToQLab.Tests");
        var testFiles = Directory.EnumerateFiles(testRoot, "*Tests.cs", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .ToHashSet(StringComparer.Ordinal);

        var declaration = new Regex(
            @"^(?:public|internal)\s+(?:(?:abstract|sealed|static|partial|readonly)\s+)*(?:class|record(?:\s+struct)?|struct|interface|enum)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)",
            RegexOptions.Multiline | RegexOptions.CultureInvariant);

        var missing = productionRoots
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .SelectMany(file => declaration.Matches(File.ReadAllText(file)).Select(match => match.Groups["name"].Value))
            .Distinct(StringComparer.Ordinal)
            .Where(typeName => !testFiles.Contains($"{typeName}Tests.cs"))
            .OrderBy(typeName => typeName, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            missing.Length == 0,
            $"Production types without a dedicated test file:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    private static string RemoveGenericArity(string typeName)
    {
        var arityMarker = typeName.IndexOf('`');
        return arityMarker < 0 ? typeName : typeName[..arityMarker];
    }

    private static string FindRepositoryRoot([CallerFilePath] string sourceFile = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(sourceFile)!);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EosToQLab.slnx")))
            directory = directory.Parent;

        return directory?.FullName
               ?? throw new DirectoryNotFoundException("Could not locate the EosToQLab repository root.");
    }
}
using System.Text.Json;
using EosToQLab.Core.Models;

namespace EosToQLab.Infrastructure.QLab;

internal static class QLabJsonParser
{
    private static readonly string[] WorkspaceIdFields = ["uniqueID", "uniqueId", "id", "workspace_id"];
    private static readonly string[] WorkspaceNameFields = ["displayName", "name", "workspaceName"];
    private static readonly string[] WorkspacePathFields = ["path", "filePath", "basePath"];
    private static readonly string[] CueIdFields = ["uniqueID", "uniqueId", "id"];
    private static readonly string[] CueNameFields = ["name", "displayName"];
    private static readonly string[] CueNumberFields = ["number", "qNumber"];
    private static readonly string[] PatchIdFields = ["uniqueID", "uniqueId", "id", "patchID"];
    private static readonly string[] PatchNameFields = ["name", "displayName"];
    private static readonly string[] PatchTypeFields = ["type", "patchType", "networkType", "destinationType"];

    internal static IReadOnlyList<QLabWorkspace> ParseWorkspaces(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Array) return [];

        var result = new List<QLabWorkspace>();
        foreach (var item in data.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) continue;

            var id = GetPropertyString(item, WorkspaceIdFields);
            if (string.IsNullOrWhiteSpace(id)) continue;

            var name = GetPropertyString(item, WorkspaceNameFields) ?? id;
            var path = GetPropertyString(item, WorkspacePathFields);
            result.Add(new QLabWorkspace(id, name, path));
        }

        return result;
    }

    internal static IReadOnlyList<QLabCueList> ParseCueLists(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Array) return [];

        var result = new List<QLabCueList>();
        foreach (var item in data.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) continue;

            var id = GetPropertyString(item, CueIdFields);
            if (string.IsNullOrWhiteSpace(id)) continue;

            result.Add(new QLabCueList(
                id,
                GetPropertyString(item, CueNameFields) ?? string.Empty,
                GetPropertyString(item, CueNumberFields)));
        }

        return result;
    }

    internal static IReadOnlyList<QLabNetworkPatch> ParseNetworkPatches(JsonElement data)
    {
        var result = new List<QLabNetworkPatch>();
        if (data.ValueKind == JsonValueKind.Array)
            foreach (var item in data.EnumerateArray())
                AddPatch(result, item, null);
        else if (data.ValueKind == JsonValueKind.Object)
            foreach (var property in data.EnumerateObject())
                AddPatch(result, property.Value, property.Name);

        return result;
    }

    internal static string? GetString(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static void AddPatch(ICollection<QLabNetworkPatch> result, JsonElement item, string? fallbackId)
    {
        if (item.ValueKind != JsonValueKind.Object) return;

        var id = GetPropertyString(item, PatchIdFields) ?? fallbackId;
        var name = GetPropertyString(item, PatchNameFields);
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name)) return;

        result.Add(new QLabNetworkPatch(
            id,
            name,
            GetPropertyString(item, PatchTypeFields)));
    }

    private static string? GetPropertyString(JsonElement element, IEnumerable<string> names)
    {
        foreach (var name in names)
            if (element.TryGetProperty(name, out var value))
            {
                var result = GetString(value);
                if (result is not null) return result;
            }

        return null;
    }
}
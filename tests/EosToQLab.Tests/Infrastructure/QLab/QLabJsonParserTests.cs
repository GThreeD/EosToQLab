using System.Text.Json;

namespace EosToQLab.Tests.Infrastructure.QLab;

public sealed class QLabJsonParserTests
{
    [Fact]
    public void Parses_workspaces_with_aliases_and_skips_invalid_items()
    {
        using var json =
            JsonDocument.Parse(
                """[1,{"uniqueID":"a","displayName":"A","path":"/a"},{"workspace_id":"b"},{"name":"missing"}]""");
        var values = QLabJsonParser.ParseWorkspaces(json.RootElement);
        Assert.Equal(2, values.Count);
        Assert.Equal(new QLabWorkspace("a", "A", "/a"), values[0]);
        Assert.Equal(new QLabWorkspace("b", "b", null), values[1]);
        Assert.Empty(QLabJsonParser.ParseWorkspaces(Json("{}")));
    }

    [Fact]
    public void Parses_cue_lists_with_aliases_and_defaults()
    {
        using var json =
            JsonDocument.Parse("""[{"id":"1","displayName":"List","qNumber":2},{"uniqueId":"2"},null,{}]""");
        var values = QLabJsonParser.ParseCueLists(json.RootElement);
        Assert.Equal(new QLabCueList("1", "List", "2"), values[0]);
        Assert.Equal(new QLabCueList("2", "", null), values[1]);
        Assert.Empty(QLabJsonParser.ParseCueLists(Json("true")));
    }

    [Fact]
    public void Parses_patch_arrays_and_dictionary_shapes()
    {
        using var array =
            JsonDocument.Parse(
                """[{"patchID":"p1","displayName":"Eos","destinationType":"eos"},{"name":"missing id"},1]""");
        Assert.Equal([new QLabNetworkPatch("p1", "Eos", "eos")], QLabJsonParser.ParseNetworkPatches(array.RootElement));

        using var map = JsonDocument.Parse("""{"p2":{"name":"Other","patchType":"osc"},"bad":2}""");
        Assert.Equal([new QLabNetworkPatch("p2", "Other", "osc")], QLabJsonParser.ParseNetworkPatches(map.RootElement));
        Assert.Empty(QLabJsonParser.ParseNetworkPatches(Json("null")));
    }

    [Theory]
    [InlineData("\"text\"", "text")]
    [InlineData("12.5", "12.5")]
    [InlineData("true", "true")]
    [InlineData("false", "false")]
    [InlineData("null", null)]
    [InlineData("[]", null)]
    public void GetString_normalizes_supported_json_values(string json, string? expected)
    {
        Assert.Equal(expected, QLabJsonParser.GetString(Json(json)));
    }

    private static JsonElement Json(string text)
    {
        return JsonDocument.Parse(text).RootElement.Clone();
    }
}
namespace EosToQLab.Tests.Core.Models;

public sealed class EosImportRequestTests
{
    [Fact]
    public void Stores_constructor_values()
    {
        var stream = new MemoryStream();
        var value = new EosImportRequest("show.csv", stream);
        Assert.Equal("show.csv", value.FileName);
        Assert.Same(stream, value.Content);
    }
}
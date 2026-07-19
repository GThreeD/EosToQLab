namespace EosToQLab.Tests.Infrastructure.Import.Csv;

public sealed class EosCsvColumnAttributeTests
{
    private static readonly string[] Expected = ["OLD"];

    [Fact]
    public void Defaults_and_initializers_are_exposed()
    {
        var attribute = new EosCsvColumnAttribute("COLUMN") { Required = true, Trim = false, Aliases = ["OLD"] };
        Assert.Equal("COLUMN", attribute.Name);
        Assert.True(attribute.Required);
        Assert.False(attribute.Trim);
        Assert.Equal(Expected, attribute.Aliases);
    }
}
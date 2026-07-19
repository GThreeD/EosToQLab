namespace EosToQLab.Tests.Core.Import;

public sealed class IEosCueImporterTests
{
    [Fact]
    public void Defines_import_contract()
    {
        Assert.True(typeof(IEosCueImporter).IsInterface);
    }
}
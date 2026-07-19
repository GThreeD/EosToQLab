namespace EosToQLab.Tests.Core.Import;

public sealed class IEosCueImporterFactoryTests
{
    [Fact]
    public void Defines_importer_factory_contract()
    {
        Assert.True(typeof(IEosCueImporterFactory).IsInterface);
    }
}
namespace EosToQLab.Tests.Core.Planning;

public sealed class IQLabImportPlanBuilderTests
{
    [Fact]
    public void Concrete_builder_implements_contract()
    {
        Assert.IsAssignableFrom<IQLabImportPlanBuilder>(new QLabImportPlanBuilder());
    }
}
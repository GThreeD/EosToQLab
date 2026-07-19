namespace EosToQLab.Tests.Infrastructure.QLab.Workflow;

public sealed class IQLabPlanItemMapperTests
{
    [Fact]
    public void Concrete_mapper_implements_contract()
    {
        Assert.IsAssignableFrom<IQLabPlanItemMapper>(new QLabMemoCuePlanMapper());
    }
}
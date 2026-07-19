namespace EosToQLab.Tests.Infrastructure.QLab.Workflow;

public sealed class QLabPlanItemMapperTests
{
    [Fact]
    public void Exposes_supported_type_maps_typed_item_and_validates_arguments()
    {
        IQLabPlanItemMapper mapper = new TestMapper();
        var context = new QLabPlanExecutionContext(new QLabNetworkPatch("id", "Eos", "eos"));
        Assert.Equal(typeof(QLabMemoCuePlan), mapper.PlanItemType);
        Assert.Equal("Scene", mapper.Map(new QLabMemoCuePlan("Scene", null), context).Name);
        Assert.Throws<ArgumentNullException>(() => mapper.Map(null!, context));
        Assert.Throws<ArgumentNullException>(() => mapper.Map(new QLabMemoCuePlan("Scene", null), null!));
        Assert.Throws<ArgumentException>(() => mapper.Map(new QLabNetworkCuePlan("Cue", "1", "1", "1", null), context));
    }

    private sealed class TestMapper : QLabPlanItemMapper<QLabMemoCuePlan>
    {
        protected override QLabCueCreationRequest Map(QLabMemoCuePlan item, QLabPlanExecutionContext context)
        {
            return new QLabCueCreationRequest(QLabCueType.Memo, item.Name, [], []);
        }
    }
}
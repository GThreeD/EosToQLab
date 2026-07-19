using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Infrastructure.QLab.Workflow;

public sealed class QLabImportPlanExecutorTests
{
    private static readonly QLabNetworkPatch Patch = new("patch-id", "Eos", "eos");

    [Fact]
    public void Constructor_rejects_null_and_duplicate_mappers()
    {
        Assert.Throws<ArgumentNullException>(() => new QLabImportPlanExecutor(null!));
        Assert.Throws<InvalidOperationException>(() =>
            new QLabImportPlanExecutor([new QLabMemoCuePlanMapper(), new QLabMemoCuePlanMapper()]));
    }

    [Fact]
    public async Task Execute_validates_arguments_and_unsupported_items()
    {
        var executor = new QLabImportPlanExecutor([]);
        var session = new FakeQLabOscSession();
        var context = new QLabPlanExecutionContext(Patch);
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            executor.ExecuteAsync(null!, new QLabImportPlan([]), context, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            executor.ExecuteAsync(session, null!, context, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            executor.ExecuteAsync(session, new QLabImportPlan([]), null!, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<QLabUnsupportedPlanItemException>(() => executor.ExecuteAsync(session,
            new QLabImportPlan([new QLabMemoCuePlan("Scene", null)]), context, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Executes_memo_and_network_requests_in_order_and_verifies_patch_once()
    {
        var session = new FakeQLabOscSession();
        var executor = new QLabImportPlanExecutor([new QLabMemoCuePlanMapper(), new QLabNetworkCuePlanMapper()]);
        var plan = new QLabImportPlan([
            new QLabMemoCuePlan("Scene", null),
            new QLabNetworkCuePlan("Cue 1", "1", "1", "1", null),
            new QLabNetworkCuePlan("Cue 2", "1", "2", "2", null)
        ]);

        var result = await executor.ExecuteAsync(session, plan, new QLabPlanExecutionContext(Patch),
            TestContext.Current.CancellationToken);

        Assert.Equal(3, session.CreatedCues.Count);
        Assert.Equal(10, session.NetworkParameterWrites.Count);
        Assert.Equal([0, 1, 2, 3, 4], session.NetworkParameterWrites.Take(5).Select(x => x.Index));
        Assert.Equal(2, result.PendingCueNumbers.Count);
        Assert.Equal(3,
            session.CuePropertyWrites.Count(x =>
                x.Property == QLabCueProperty.Number && Equals(x.Value, string.Empty)));
        Assert.Equal(1, session.QueryCount);
    }

    [Fact]
    public async Task Empty_desired_number_does_not_clear_or_queue_number()
    {
        var session = new FakeQLabOscSession();
        var executor = new QLabImportPlanExecutor([
            new RequestMapper(new QLabCueCreationRequest(QLabCueType.Memo, "Cue", [], [], DesiredCueNumber: " "))
        ]);
        var result = await executor.ExecuteAsync(session, new QLabImportPlan([new QLabMemoCuePlan("Scene", null)]),
            new QLabPlanExecutionContext(Patch), TestContext.Current.CancellationToken);
        Assert.Empty(result.PendingCueNumbers);
        Assert.DoesNotContain(session.CuePropertyWrites, x => x.Property == QLabCueProperty.Number);
    }

    [Fact]
    public async Task Rejects_non_contiguous_network_parameters_and_wraps_non_domain_failures()
    {
        var request = new QLabCueCreationRequest(QLabCueType.Network, "Cue", [],
            [new QLabNetworkParameterAssignment(1, QLabEosParameter.Type, "Cues")]);
        var executor = new QLabImportPlanExecutor([new RequestMapper(request)]);
        var exception = await Assert.ThrowsAsync<QLabCueCreationException>(() =>
            executor.ExecuteAsync(new FakeQLabOscSession(), new QLabImportPlan([new QLabMemoCuePlan("Scene", null)]),
                new QLabPlanExecutionContext(Patch), TestContext.Current.CancellationToken));
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public async Task Patch_mismatch_is_a_domain_error_and_is_not_wrapped()
    {
        var session = new FakeQLabOscSession
            { QueryOverride = (_, property) => property == QLabCueProperty.NetworkPatchId ? "other" : null };
        var executor = new QLabImportPlanExecutor([new QLabNetworkCuePlanMapper()]);
        await Assert.ThrowsAsync<QLabNetworkPatchAssignmentException>(() => executor.ExecuteAsync(session,
            new QLabImportPlan([new QLabNetworkCuePlan("Cue", "1", "1", "1", null)]),
            new QLabPlanExecutionContext(Patch), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task General_create_and_parameter_failures_are_wrapped_but_cancellation_is_not()
    {
        var executor = new QLabImportPlanExecutor([new QLabMemoCuePlanMapper()]);
        var createFailure = new FakeQLabOscSession { CreateFailure = new InvalidOperationException("broken") };
        await Assert.ThrowsAsync<QLabCueCreationException>(() => executor.ExecuteAsync(createFailure,
            new QLabImportPlan([new QLabMemoCuePlan("Scene", null)]), new QLabPlanExecutionContext(Patch),
            TestContext.Current.CancellationToken));

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executor.ExecuteAsync(new FakeQLabOscSession(),
            new QLabImportPlan([new QLabMemoCuePlan("Scene", null)]), new QLabPlanExecutionContext(Patch),
            cancellation.Token));
    }

    [Fact]
    public async Task AssignCueNumbers_counts_successes_and_ignores_number_conflicts()
    {
        var session = new FakeQLabOscSession
        {
            SetCuePropertyFailure = (_, property, value) => property == QLabCueProperty.Number && Equals(value, "2")
                ? new QLabUnexpectedReplyException("/number", "error")
                : null
        };
        var count = await QLabImportPlanExecutor.AssignCueNumbersAsync(session, [
            new QLabPendingCueNumberAssignment("1", "Cue 1", "1"), new QLabPendingCueNumberAssignment("2", "Cue 2", "2")
        ], TestContext.Current.CancellationToken);
        Assert.Equal(1, count);
        Assert.Contains(session.CuePropertyWrites, x => Equals(x.Value, "1"));
        Assert.DoesNotContain(session.CuePropertyWrites, x => Equals(x.Value, "2"));
    }

    [Fact]
    public async Task Fatal_number_assignment_undoes_only_previous_successes()
    {
        var session = new FakeQLabOscSession
        {
            SetCuePropertyFailure = (_, property, value) => property == QLabCueProperty.Number && Equals(value, "2")
                ? new InvalidOperationException("fatal")
                : null
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => QLabImportPlanExecutor.AssignCueNumbersAsync(session,
        [
            new QLabPendingCueNumberAssignment("1", "Cue 1", "1"), new QLabPendingCueNumberAssignment("2", "Cue 2", "2")
        ], TestContext.Current.CancellationToken));
        Assert.Equal(1, session.UndoCount);
    }

    [Fact]
    public async Task AssignCueNumbers_validates_arguments()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            QLabImportPlanExecutor.AssignCueNumbersAsync(null!, [], TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            QLabImportPlanExecutor.AssignCueNumbersAsync(new FakeQLabOscSession(), null!,
                TestContext.Current.CancellationToken));
    }

    private sealed class RequestMapper(QLabCueCreationRequest request) : QLabPlanItemMapper<QLabMemoCuePlan>
    {
        protected override QLabCueCreationRequest Map(QLabMemoCuePlan item, QLabPlanExecutionContext context)
        {
            return request;
        }
    }
}
using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Infrastructure.QLab.Workflow;

public sealed class QLabImportWorkflowTests
{
    [Fact]
    public async Task Validates_arguments()
    {
        var workflow = Create(new FakeQLabOscSession());
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            workflow.ExecuteAsync(null!, TestData.Options(), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            workflow.ExecuteAsync([], null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Creates_import_list_assigns_exact_numbers_and_saves()
    {
        var session = new FakeQLabOscSession();
        var service = new FakeQLabOscService(session);
        var workflow = Create(session, service);

        var result = await workflow.ExecuteAsync([TestData.Cue(label: "Cue")],
            TestData.Options() with { Passcode = "secret" }, TestContext.Current.CancellationToken);

        Assert.Equal("workspace", result.WorkspaceId);
        Assert.Equal("cue-1", result.CueListId);
        Assert.Equal(1, result.NetworkCueCount);
        Assert.False(result.ReplacedExistingCueList);
        Assert.Equal(("workspace", "secret"), Assert.Single(service.Connections));
        Assert.Equal(1, session.SaveCount);
        Assert.Contains(session.CuePropertyWrites, x => x is { Property: QLabCueProperty.Number, Value: "1" });
        Assert.Equal(1, session.DisposeCount);
    }

    [Fact]
    public async Task Can_import_without_saving_workspace()
    {
        var session = new FakeQLabOscSession();
        var result = await Create(session).ExecuteAsync([TestData.Cue()],
            TestData.Options() with { SaveWorkspaceAfterImport = false }, TestContext.Current.CancellationToken);
        Assert.Equal(0, session.SaveCount);
        Assert.False(result.ReplacedExistingCueList);
    }

    [Fact]
    public async Task Conflict_requires_explicit_replace_consent()
    {
        var session = new FakeQLabOscSession { CueLists = [new QLabCueList("old", "Imported", null)] };
        await Assert.ThrowsAsync<QLabCueListConflictException>(() =>
            Create(session).ExecuteAsync([TestData.Cue()], TestData.Options(), TestContext.Current.CancellationToken));
        Assert.Empty(session.CreatedCues);
    }

    [Fact]
    public async Task Replace_renames_saves_deletes_then_assigns_numbers()
    {
        var session = new FakeQLabOscSession { CueLists = [new QLabCueList("old", "Imported", null)] };
        var options = TestData.Options() with
        {
            ConflictPolicy = CueListConflictPolicy.ReplaceWithExplicitConsent,
            ExplicitReplacementConsent = true
        };
        var result = await Create(session)
            .ExecuteAsync([TestData.Cue()], options, TestContext.Current.CancellationToken);

        Assert.True(result.ReplacedExistingCueList);
        Assert.Equal(2, session.SaveCount);
        Assert.Contains(session.Renames, x => x.CueListId == "old" && x.TargetName.Contains("backup"));
        Assert.Contains(session.Deletes, x => x.CueListId == "old");
        Assert.Contains(session.CuePropertyWrites, x => x is { Property: QLabCueProperty.Number, Value: "1" });
    }

    [Fact]
    public async Task Rejects_missing_or_wrong_patch_before_modifying_workspace()
    {
        var missing = new FakeQLabOscSession { NetworkPatches = [] };
        await Assert.ThrowsAsync<QLabNetworkPatchNotFoundException>(() =>
            Create(missing).ExecuteAsync([], TestData.Options(), TestContext.Current.CancellationToken));
        Assert.Empty(missing.CreatedCues);

        var wrong = new FakeQLabOscSession
            { NetworkPatches = [new QLabNetworkPatch("patch-id", "Eos", "generic osc")] };
        await Assert.ThrowsAsync<QLabNetworkPatchTypeMismatchException>(() =>
            Create(wrong).ExecuteAsync([], TestData.Options(), TestContext.Current.CancellationToken));
        Assert.Empty(wrong.CreatedCues);
    }

    [Fact]
    public async Task Failure_after_temporary_creation_restores_current_list_and_deletes_temporary_list()
    {
        var session = new FakeQLabOscSession { NetworkParameterFailure = new InvalidOperationException("broken") };
        await Assert.ThrowsAsync<QLabCueCreationException>(() =>
            Create(session).ExecuteAsync([TestData.Cue()], TestData.Options(), TestContext.Current.CancellationToken));

        Assert.Contains(session.Renames,
            x => x.TargetName.StartsWith("EosToQLab failed import", StringComparison.Ordinal));
        Assert.Contains(session.WorkspacePropertyWrites, x => Equals(x.Value, "original-list"));
        Assert.Contains(session.Deletes, x => x.CueListId == "cue-1");
    }

    [Fact]
    public async Task Failure_after_replacement_deletion_undoes_numbers_and_deletion_before_restoring_backup()
    {
        var session = new FakeQLabOscSession
        {
            CueLists = [new QLabCueList("old", "Imported", null)],
            SaveFailure = new InvalidOperationException("final save failed"),
            SaveFailureOnAttempt = 2
        };
        var options = TestData.Options() with
        {
            ConflictPolicy = CueListConflictPolicy.ReplaceWithExplicitConsent,
            ExplicitReplacementConsent = true
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Create(session).ExecuteAsync([TestData.Cue()], options, TestContext.Current.CancellationToken));

        Assert.Equal(2, session.UndoCount);
        Assert.Contains(session.Renames, x => x is { CueListId: "old", TargetName: "Imported" });
        Assert.True(session.SaveCount >= 2);
    }

    [Fact]
    public async Task Rollback_failure_is_wrapped_with_original_failure()
    {
        var session = new FakeQLabOscSession
        {
            NetworkParameterFailure = new InvalidOperationException("import failed"),
            DeleteFailure = (_, _) => new InvalidOperationException("rollback failed")
        };
        var exception = await Assert.ThrowsAsync<QLabImportRollbackException>(() =>
            Create(session).ExecuteAsync([TestData.Cue()], TestData.Options(), TestContext.Current.CancellationToken));
        Assert.IsType<AggregateException>(exception.InnerException);
    }

    private static QLabImportWorkflow Create(FakeQLabOscSession session, FakeQLabOscService? service = null)
    {
        return new QLabImportWorkflow(service ?? new FakeQLabOscService(session), new QLabImportPlanBuilder(),
            new QLabImportPlanExecutor([new QLabMemoCuePlanMapper(), new QLabNetworkCuePlanMapper()]));
    }
}
namespace EosToQLab.Tests.TestDoubles;

internal sealed class FakeQLabOscSession : IQLabOscSession
{
    private int _cueSequence;
    public IReadOnlyList<QLabCueList> CueLists { get; set; } = [];
    public IReadOnlyList<QLabNetworkPatch> NetworkPatches { get; set; } = [new("patch-id", "Eos", "eos")];
    public string? CurrentCueListId { get; set; } = "original-list";
    public Dictionary<(string CueId, QLabCueProperty Property), string?> QueryValues { get; } = [];
    public List<(QLabCueType Type, string Name)> CreatedCues { get; } = [];
    public List<(string CueId, QLabCueProperty Property, object? Value)> CuePropertyWrites { get; } = [];
    public List<(QLabWorkspaceProperty Property, object? Value)> WorkspacePropertyWrites { get; } = [];
    public List<(string CueId, int Index, string Value)> NetworkParameterWrites { get; } = [];
    public List<(string CueListId, string CurrentName, string TargetName)> Renames { get; } = [];
    public List<(string CueListId, string Name)> Deletes { get; } = [];
    public int SaveCount { get; private set; }
    public int SaveAttemptCount { get; private set; }
    public int UndoCount { get; private set; }
    public int DisposeCount { get; private set; }
    public int QueryCount { get; private set; }
    public Func<string, QLabCueProperty, object?, Exception?>? SetCuePropertyFailure { get; set; }
    public Func<string, string, string, Exception?>? RenameFailure { get; set; }
    public Func<string, string, Exception?>? DeleteFailure { get; set; }
    public Exception? SaveFailure { get; set; }
    public int? SaveFailureOnAttempt { get; set; }
    public Exception? CreateFailure { get; set; }
    public Exception? NetworkParameterFailure { get; set; }
    public Exception? UndoFailure { get; set; }
    public Func<string, QLabCueProperty, string?>? QueryOverride { get; set; }
    public QLabWorkspace Workspace { get; set; } = new("workspace", "Workspace", null);

    public Task<IReadOnlyList<QLabCueList>> GetCueListsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CueLists);
    }

    public Task<IReadOnlyList<QLabNetworkPatch>> GetNetworkPatchesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(NetworkPatches);
    }

    public Task<string?> GetCurrentCueListIdAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CurrentCueListId);
    }

    public Task<string> CreateCueAsync(QLabCueType cueType, string cueName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (CreateFailure is not null) throw CreateFailure;
        CreatedCues.Add((cueType, cueName));
        return Task.FromResult($"cue-{++_cueSequence}");
    }

    public Task SetCuePropertyAsync(string cueId, QLabCueProperty cueProperty, object? value,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var failure = SetCuePropertyFailure?.Invoke(cueId, cueProperty, value);
        if (failure is not null) throw failure;
        CuePropertyWrites.Add((cueId, cueProperty, value));
        QueryValues[(cueId, cueProperty)] = value?.ToString();
        return Task.CompletedTask;
    }

    public Task SetWorkspacePropertyAsync(QLabWorkspaceProperty cueProperty, object? value,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        WorkspacePropertyWrites.Add((cueProperty, value));
        return Task.CompletedTask;
    }

    public Task SetNetworkParameterAsync(string cueId, int parameterIndex, string value,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (NetworkParameterFailure is not null) throw NetworkParameterFailure;
        NetworkParameterWrites.Add((cueId, parameterIndex, value));
        return Task.CompletedTask;
    }

    public Task<string?> QueryCuePropertyAsync(string cueId, QLabCueProperty cueProperty,
        CancellationToken cancellationToken = default)
    {
        QueryCount++;
        if (QueryOverride is not null) return Task.FromResult(QueryOverride(cueId, cueProperty));
        QueryValues.TryGetValue((cueId, cueProperty), out var value);
        return Task.FromResult(value);
    }

    public Task RenameCueListAsync(string cueListId, string currentName, string targetName,
        CancellationToken cancellationToken = default)
    {
        var failure = RenameFailure?.Invoke(cueListId, currentName, targetName);
        if (failure is not null) throw failure;
        Renames.Add((cueListId, currentName, targetName));
        return Task.CompletedTask;
    }

    public Task DeleteCueListAsync(string cueListId, string cueListName, CancellationToken cancellationToken = default)
    {
        var failure = DeleteFailure?.Invoke(cueListId, cueListName);
        if (failure is not null) throw failure;
        Deletes.Add((cueListId, cueListName));
        return Task.CompletedTask;
    }

    public Task SaveWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        SaveAttemptCount++;
        if (SaveFailure is not null && (SaveFailureOnAttempt is null || SaveFailureOnAttempt == SaveAttemptCount))
            throw SaveFailure;
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task UndoAsync(CancellationToken cancellationToken = default)
    {
        if (UndoFailure is not null) throw UndoFailure;
        UndoCount++;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        DisposeCount++;
        return ValueTask.CompletedTask;
    }
}

internal sealed class FakeQLabOscService(FakeQLabOscSession session) : IQLabOscService
{
    public IReadOnlyList<QLabWorkspace> Workspaces { get; set; } = [session.Workspace];
    public List<(string WorkspaceId, string? Passcode)> Connections { get; } = [];
    public Exception? ConnectFailure { get; set; }

    public Task<IReadOnlyList<QLabWorkspace>> GetOpenWorkspacesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Workspaces);
    }

    public Task<IQLabOscSession> ConnectWorkspaceAsync(string workspaceId, string? passcode,
        CancellationToken cancellationToken = default)
    {
        if (ConnectFailure is not null) throw ConnectFailure;
        Connections.Add((workspaceId, passcode));
        return Task.FromResult<IQLabOscSession>(session);
    }
}
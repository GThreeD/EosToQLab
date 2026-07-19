# QLab safety and conflict handling

## Workspace boundary

EosToQLab only works with workspaces returned by QLab's `/workspaces` OSC command. It does not:

- create a new QLab document;
- open a QLab document;
- choose a QLab file path;
- perform Save As.

The user opens the desired workspace manually before running the import.

## Default policy

`QLabImportOptions.ConflictPolicy` defaults to `Fail`.

If any existing cue list has the requested name, compared case-insensitively, `QLabCueListConflictException` is thrown
unless both conditions are true:

- policy is `ReplaceWithExplicitConsent`;
- `ExplicitReplacementConsent` is `true`.

The service enforces this independently from the UI, so a future UI cannot accidentally bypass it.

## New-list workflow

For a non-conflicting name:

1. create a cue list with a unique temporary name;
2. make it current;
3. create every Memo and Network cue and clear any automatically assigned QLab number;
4. verify Network Patch assignment for each Network cue;
5. rename the list to the final name and read the name back for verification;
6. assign each Network cue exactly its EOS cue number, leaving it unnumbered if QLab reports a conflict;
7. save only after all previous steps succeed, if requested.

On failure, the previous current cue list is restored and the temporary list is deleted.

## Explicit replacement workflow

For an explicitly approved conflict:

1. build the new list completely under a temporary name;
2. rename the old list to a GUID-based backup name and verify the rename;
3. rename the new list to the requested name and verify the rename;
4. when automatic saving is enabled, save a recoverable state containing both lists;
5. delete the backup list and verify that its ID is no longer present;
6. assign each Network cue exactly its EOS cue number, leaving it unnumbered if the number is still used elsewhere in
   the workspace;
7. save the final state.

The old list is therefore not deleted until the new list has been completely populated.

## Rollback

If replacement fails after the old list was deleted, the service first undoes any successful post-deletion cue-number
assignments and then requests one additional QLab workspace undo to restore the deleted list. It then:

- gives the failed new list a unique non-conflicting name;
- restores the old list name;
- restores the previously current cue list;
- deletes the failed new list;
- saves the restored state if an intermediate recoverable state had already been saved.

If rollback itself fails, `QLabImportRollbackException` includes both the original import failure and rollback failure
as an aggregate inner exception. The user should then inspect the QLab workspace before saving or closing it.

## Remaining integration requirement

OSC behavior must be validated against the exact installed QLab 5 version on macOS. The included self-tests validate
parsers, factory selection, Follow/Hang planning, and scene handling, but they do not emulate QLab's live OSC server.

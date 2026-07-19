# Build and validation

## Restore

```bash
dotnet workload install maui
dotnet restore EosToQLab.sln
```

## Cross-platform source tests

The self-test console project has no external test-framework dependency. It checks:

- factory selection for `.csv` and `.esf3d`;
- mapping of the reference EOS CSV;
- cue-part aggregation;
- fixed scene-text, label, and cue-notes mapping;
- the synthetic ESF3D fixture, including cue notes and Follow/Hang;
- corrected Follow/Hang planning behavior;
- scene Memo-cue de-duplication and empty Memo notes.

```bash
dotnet run --project tests/EosToQLab.SelfTests/EosToQLab.SelfTests.csproj -c Release
```

## Build the macOS app

```bash
dotnet build src/EosToQLab.App/EosToQLab.App.csproj \
  -f net10.0-maccatalyst \
  -c Release
```

## Publish

```bash
./packaging/publish-macos.sh arm64
./packaging/publish-macos.sh x64
```

## Manual QLab integration test

1. Open a disposable QLab 5 workspace.
2. Enable OSC edit access.
3. Add an ETC Eos family Network Patch named `Eos`.
4. Add an existing cue list named `Protected List` with a recognizable cue.
5. Import with final name `New Light Cues`. Confirm that `Protected List` is unchanged.
6. Attempt another import with final name `Protected List`. Confirm that the service rejects it before creating a replacement unless explicit consent is selected.
7. Approve replacement in a disposable copy of the workspace. Confirm that the old list remains until the new temporary list is fully populated.
8. Check Memo cues, Network-cue names, patch assignment, and parameter values.
9. Test with an incorrect OSC passcode and verify `QLabAccessDeniedException`.
10. Test without an open workspace and verify `QLabNoOpenWorkspaceException`.

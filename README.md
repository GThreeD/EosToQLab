# EosToQLab

A small macOS desktop application built with C#, .NET 10, .NET MAUI, and Blazor Hybrid. It imports ETC Eos cue data from either an EOS CSV export or an `.esf3d` archive and creates QLab 5 Network cues in an **already open** QLab workspace.

The application does not export an intermediate file and does not use AppleScript.

## Main behavior

- Accepts `.csv` and `.esf3d` by drag and drop or file selection.
- Selects the import strategy through `IEosCueImporterFactory`.
- Both source implementations implement the same `IEosCueImporter` contract.
- Maps both formats into the same `EosCue` domain model.
- Uses an attribute-bound `EosCsvCue` source model for EOS CSV columns.
- Opens `.esf3d` as ZIP and reads `showdat.dat` directly from the archive.
- Uses QLab's OSC-over-TCP interface on `127.0.0.1:53000`.
- Only lists and targets QLab workspaces that are already open.
- Never creates, opens, or saves-as a new QLab document.
- Creates a separate temporary cue list before touching an existing list.
- Rejects a duplicate cue-list name by default.
- Allows replacement only after explicit confirmation in the UI and a second enforcement check in the QLab service.
- Maps EOS scene text to QLab Memo-cue names; generated Memo notes stay empty.
- Maps EOS labels to Network-cue names and EOS cue notes to Network-cue notes without generated prefixes.
- Corrects the Follow/Hang handling, reads `FOLLOW` by column name in CSV, and decodes known ESF3D follow/hang encodings.
- Uses dedicated exception and warning types with English messages and stable diagnostic codes.

## Small project structure

```text
src/
├── EosToQLab.Core
│   ├── Models
│   ├── Import contracts
│   ├── QLab contracts
│   ├── Planning
│   ├── Diagnostics
│   └── Exceptions
├── EosToQLab.Infrastructure
│   ├── CSV import
│   ├── ESF3D import
│   └── QLab OSC
└── EosToQLab.App
    └── MAUI Blazor Hybrid UI for macOS

tests/
└── EosToQLab.SelfTests
```

The structure is intentionally limited to three production projects. There is no separate project for every technical layer.

## Import strategy factory

```csharp
public interface IEosCueImporter
{
    EosSourceKind SourceKind { get; }
    bool CanImport(string fileName);
    Task<EosImportResult> ImportAsync(
        EosImportRequest request,
        CancellationToken cancellationToken = default);
}
```

`CsvEosCueImporter` and `Esf3dEosCueImporter` implement this interface. `EosCueImporterFactory` selects the first registered strategy whose `CanImport` method accepts the file.

## CSV mapping

`EosCsvCue` is a transport/source model. Its properties are associated with EOS column names using `EosCsvColumnAttribute`:

```csharp
[EosCsvColumn("TARGET_ID", Required = true)]
public string TargetId { get; init; } = string.Empty;

[EosCsvColumn("SCENE_TEXT")]
public string? SceneText { get; init; }
```

The binder caches reflection metadata once per source type, reads values by header name, converts them, and then maps the source rows into the common `EosCue` model. Cue-part rows are grouped before mapping.

## ESF3D parser scope

`showdat.dat` is a proprietary format. The included parser is intentionally loss-tolerant and is based on the supplied Python reference parser. It currently recovers:

- cue numbers encoded as fixed-point values with scale 10,000;
- cue labels directly following the encoded cue number;
- cue notes from the dedicated cue-header field;
- nested scene labels;
- known scalar, textual, and compact-object Follow/Hang encodings;
- other short text fields as additional diagnostic data.

Unknown Follow/Hang object variants are not guessed. They produce a cue-specific warning and remain empty instead of being interpreted as another field.

## QLab requirements

Before starting an import:

1. Start QLab 5.
2. Open the target `.qlab5` workspace manually.
3. Configure OSC edit access in that workspace. Enter the passcode in EosToQLab if required.
4. Create a Network Patch of type **ETC Eos family**. The default expected patch name is exactly `Eos`.
5. Start EosToQLab and refresh the list of open workspaces.

The app does not attempt to create or open a QLab file.

## Existing cue-list protection

The default conflict policy is `Fail`. A matching name is checked twice:

1. in the Blazor UI before the import starts;
2. again inside `QLabOscService` immediately before any cue list is created.

For an explicitly approved replacement, the service:

1. builds the complete new list under a unique temporary name;
2. renames the existing list to a unique backup name;
3. renames the new list to the requested final name;
4. optionally saves a recoverable state containing both lists;
5. deletes the backup list;
6. optionally saves the final state.

If a later step fails, the service attempts to restore the old name, restore the previous current cue list, remove the temporary import, and save the rollback where necessary.

See [docs/QLAB_SAFETY.md](docs/QLAB_SAFETY.md).

## Development prerequisites

- macOS with a supported Xcode version
- .NET SDK `10.0.302` or a compatible later patch
- .NET MAUI workload
- QLab 5 for integration testing

```bash
dotnet workload install maui
dotnet restore EosToQLab.sln
```

Run the source/import self-tests:

```bash
dotnet run --project tests/EosToQLab.SelfTests/EosToQLab.SelfTests.csproj -c Release
```

Run the app:

```bash
dotnet build src/EosToQLab.App/EosToQLab.App.csproj \
  -f net10.0-maccatalyst \
  -t:Run
```

## Publish a self-contained `.app`

Apple Silicon:

```bash
./packaging/publish-macos.sh arm64
```

Intel:

```bash
./packaging/publish-macos.sh x64
```

Artifacts are copied to `dist/`. Local output is ad-hoc signed. Public distribution requires an Apple Developer ID and notarization.

## Documentation

- [Architecture](docs/ARCHITECTURE.md)
- [QLab safety and conflict handling](docs/QLAB_SAFETY.md)
- [Build and validation](docs/BUILD_AND_TEST.md)

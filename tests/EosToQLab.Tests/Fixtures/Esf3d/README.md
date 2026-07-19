# EosToQLab

A small macOS desktop application built with C#, .NET 10, .NET MAUI, and Blazor Hybrid. It imports ETC Eos cue data from
either an EOS CSV export or an `.esf3d` archive and creates QLab 5 Network cues in an **already open** QLab workspace.

The application does not export an intermediate file and does not use AppleScript.

## Main behavior

- Accepts `.csv` and `.esf3d` through the native macOS file picker. The app registers `.esf3d` as an imported document
  type so the extension remains selectable.
- Selects the import strategy through `IEosCueImporterFactory`.
- Both source implementations implement the same `IEosCueImporter` contract.
- Maps both formats into the same `EosCue` domain model.
- Uses an attribute-bound `EosCsvCue` source model for EOS CSV columns.
- Opens `.esf3d` as ZIP and reads `showdat.dat` directly from the archive.
- Uses QLab's OSC-over-TCP interface on `127.0.0.1:53000`.
- Only lists and targets QLab workspaces that are already open.
- Never creates, opens, or saves-as a new QLab document.
- Creates a separate temporary cue list before touching an existing list.
- Creates imported cues without a QLab number first, then assigns each Network cue exactly its EOS cue number. If that
  number is already used elsewhere in the workspace, the QLab cue stays unnumbered; the importer never increments or
  invents a replacement number. The EOS list/cue values sent by the Network cue are unchanged.
- Rejects a duplicate cue-list name by default.
- Allows replacement only after explicit confirmation in the UI and a second enforcement check in the QLab service.
- Maps EOS scene text to QLab Memo-cue names; generated Memo notes stay empty.
- Maps EOS labels to Network-cue names and EOS cue notes to Network-cue notes without generated prefixes.
- Models the QLab EOS Network-cue parameter stack explicitly as Type, Specify user, optional User, Command, and
  command-specific arguments. The current import uses Cues → Run cue in specific list → List/Cue.
- Corrects Follow/Hang handling, reads `FOLLOW` by column name in CSV, and decodes both legacy and current ESF3D
  continuation objects. Automatically triggered cues can be excluded or imported disarmed.
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
└── EosToQLab.Application
    └── MAUI Blazor Hybrid UI for macOS

tests/
└── EosToQLab.Tests
    ├── one xUnit test file per production type
    ├── versioned ESF3D compatibility fixtures
    └── Coverlet line-coverage gate
```

The structure is intentionally limited to three production projects. There is no separate project for every technical
layer.

## Application icon

`src/EosToQLab.Application/Resources/AppIcon/appicon.svg` is the single MAUI application-icon source. It is a vector
recreation of the original EOS-to-QLab PNG artwork and contains only paths and shapes, so the MAUI resizetizer does not
depend on an installed font. The same SVG is mirrored as `.idea/icon.svg` for JetBrains Rider project branding.

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

`CsvEosCueImporter` and `Esf3dEosCueImporter` implement this interface. `EosCueImporterFactory` selects the first
registered strategy whose `CanImport` method accepts the file.

## CSV mapping

`EosCsvCue` is a transport/source model. Its properties are associated with EOS column names using
`EosCsvColumnAttribute`:

```csharp
[EosCsvColumn("TARGET_ID", Required = true)]
public string TargetId { get; init; } = string.Empty;

[EosCsvColumn("SCENE_TEXT")]
public string? SceneText { get; init; }
```

The binder caches reflection metadata once per source type, reads values by header name, converts them, and then maps
the source rows into the common `EosCue` model. Cue-part rows are grouped before mapping.

## ESF3D parser scope

`showdat.dat` is a proprietary format. The included parser is intentionally loss-tolerant and is based on the supplied
Python reference parser. It currently recovers:

- cue numbers encoded as fixed-point values with scale 10,000;
- cue labels directly following the encoded cue number;
- cue notes from the dedicated cue-header field;
- nested scene labels;
- known scalar, textual, compact-object, and current continuation-object Follow/Hang encodings;
- other short text fields as additional diagnostic data.

Unknown Follow/Hang object variants are not guessed. They produce a cue-specific warning and remain empty instead of
being interpreted as another field.

## Follow/Hang import policy

The plan builder tracks automatic playback chains separately for every EOS cue-list number. When a cue has Follow or
Hang, the next cue is considered automatically triggered; if that next cue also has Follow/Hang, the chain continues.
The UI offers two policies:

- **Do not import**: automatically triggered cues are omitted completely.
- **Import disarmed**: automatically triggered Network cues are created with `armed = false`.

The cue that starts the chain remains a normal imported cue. For example, `83.1 F1`, `83.2 F1`, `83.3 F1`, `84` affects
83.2, 83.3, and 84.

## EOS Network cue parameters

`QLabEosNetworkCommand` represents the visible EOS Network-cue fields in order. It uses string-backed target and command
value types so additional QLab menu values can be added without changing the OSC session API. For the current import it
emits:

1. Type = `Cues`
2. Specify user = `No` (or `Yes` plus a User field)
3. Command = `Run cue in specific list`
4. List = the EOS list number
5. Cue = the EOS cue number

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
6. assigns each Network cue exactly its EOS cue number when that number is now free;
7. optionally saves the final state.

If a later step fails, the service attempts to restore the old name, restore the previous current cue list, remove the
temporary import, and save the rollback where necessary.

See [docs/QLAB_SAFETY.md](docs/QLAB_SAFETY.md).

## Development prerequisites

- macOS with a supported Xcode version
- .NET SDK `10.0.302` or a compatible later patch
- .NET MAUI workload
- QLab 5 for integration testing

```bash
dotnet workload install maui
dotnet restore EosToQLab.slnx
```

Run the xUnit suite:

```bash
dotnet test tests/EosToQLab.Tests/EosToQLab.Tests.csproj -c Release
```

Run the same 100% line-coverage gate used by CI:

```bash
dotnet test tests/EosToQLab.Tests/EosToQLab.Tests.csproj -c Release \
  -p:CollectCoverage=true \
  -p:CoverletOutputFormat=cobertura \
  -p:Threshold=100 \
  -p:ThresholdType=line \
  -p:ThresholdStat=total \
  '-p:Include=[EosToQLab.Core]*,[EosToQLab.Infrastructure]*'
```

Run the app:

```bash
dotnet build src/EosToQLab.Application/EosToQLab.Application.csproj \
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

Artifacts are copied to `dist/`. Local output is ad-hoc signed. Public distribution requires an Apple Developer ID and
notarization.

## Documentation

- [Architecture](docs/ARCHITECTURE.md)
- [QLab safety and conflict handling](docs/QLAB_SAFETY.md)
- [Build and validation](docs/BUILD_AND_TEST.md)
- [Testing strategy and ESF3D compatibility](docs/TESTING.md)

## Import UI

The Mac Catalyst UI keeps each workspace's OSC passcode only in a 24-hour in-process session cache by default, loads
QLab network patches into a dropdown, and provides an editable cue preview. Passcodes are not written to disk and are
lost when the app process exits. The optional `MauiSecurePasscodeStore` remains available for signed builds that can use
the platform keychain. Label, notes, and scene text can be changed before import, and cues can be selected individually
or through a tri-state Select all control.

## Mac Catalyst icon and Rider restart troubleshooting

The MAUI icon source is `src/EosToQLab.Application/Resources/AppIcon/appicon.svg`.
Mac Catalyst references the generated asset catalog through
`XSAppIconAssets = Assets.xcassets/appicon.appiconset` in `Info.plist`.
The same SVG is stored as `.idea/icon.svg` for Rider's project icon; this is
separate from the icon displayed for the built `.app` process.

After changing the application icon, remove `bin` and `obj` before rebuilding.
If Rider is stopped while UIKit is still closing the previous Mac Catalyst
scene and the next launch fails in `UIKitMacHelper`, run:

```bash
./scripts/reset-maccatalyst-debug-state.sh
```

The script only removes debug build output and saved window/scene state. It does
not delete the whole application container.

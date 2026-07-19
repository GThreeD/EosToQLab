# Architecture

## Goals

The application uses a deliberately small architecture:

- source formats are replaceable import strategies;
- source parsers do not know about QLab;
- QLab does not know whether a cue came from CSV or ESF3D;
- the Blazor UI coordinates services but contains no parsing or OSC protocol logic.

## Projects

### EosToQLab.Core

Contains stable application concepts:

- `EosCue` and result models;
- `IEosCueImporter` and `IEosCueImporterFactory`;
- `IQLabService` as the application-facing facade;
- `IQLabImportPlanBuilder`;
- diagnostic and exception types.

It has no MAUI, archive, reflection-mapping, TCP, or JSON dependencies beyond the .NET base libraries.

### EosToQLab.Infrastructure

Contains implementation details:

- `CsvEosCueImporter`;
- `Esf3dEosCueImporter`;
- `EosCueImporterFactory`;
- OSC encoding/decoding;
- double-END SLIP framing for QLab TCP;
- `QLabService` as the application-facing facade;
- `QLabImportWorkflow` for import orchestration and rollback;
- declarative plan-item mappers and `QLabImportPlanExecutor`;
- `IQLabOscService`/`QLabOscService` and connection-bound `IQLabOscSession`/`QLabOscSession` for direct OSC communication;
- centralized protocol names in `QLabProtocol`.

### EosToQLab.App

A macOS-only .NET MAUI Blazor Hybrid application. Razor components handle the workflow:

```text
select/drop source
    -> importer factory
    -> normalized cue preview
    -> select already-open QLab workspace
    -> validate cue-list conflict
    -> build QLab import plan
    -> execute through OSC
```

## Import strategy selection

```text
IEosCueImporterFactory
    ├── CsvEosCueImporter
    └── Esf3dEosCueImporter
```

Adding another EOS source format requires:

1. implement `IEosCueImporter`;
2. register the implementation in `MauiProgram`;
3. no changes to the UI or QLab service.

## Source model versus domain model

CSV parsing uses `EosCsvCue` as a source model because its shape follows an external format. Attributes identify the column for each property. The binder is generic and metadata is cached per type.

After parsing and cue-part aggregation, source rows are mapped into `EosCue`. ESF3D recovery also maps directly into `EosCue`. Everything after that point uses only the common model.

## QLab import planning

`QLabImportPlanBuilder` converts normalized cues into an ordered list of:

- `QLabMemoCuePlan`;
- `QLabNetworkCuePlan`.

Follow/Hang state is tracked per EOS cue-list number. The state is updated for every EOS cue, including a cue that is skipped because the previous cue had Follow/Hang. This prevents the permanent-block bug from the original AppleScript.

## QLab execution layers

```text
IQLabService / QLabService
    -> QLabImportWorkflow
        -> QLabImportPlanBuilder
        -> QLabImportPlanExecutor
            -> IQLabPlanItemMapper implementations
            -> QLabCueCreationRequest
        -> QLabOscSession
    -> QLabOscService (workspace discovery and session creation)
```

The workflow owns conflict handling, temporary cue-list creation, save behavior, and rollback. It does not build OSC addresses or know QLab property strings.

Each plan-item mapper converts one plan type into a declarative `QLabCueCreationRequest`. Adding a new plan-item type requires a new mapper registration rather than modifying a central switch statement.

`QLabOscSession` contains the direct, connection-bound QLab operations. `QLabProtocol` is the single translation point from typed enums such as `QLabCueProperty.NetworkPatchId` to QLab's OSC names such as `networkPatchID`.

## QLab transport

The OSC layer uses TCP on localhost and enables `/alwaysReply`. OSC packets are framed using QLab's double-END SLIP convention. Every write is serialized and paired with a reply. QLab reply JSON is parsed by `QLabJsonParser` and converted into dedicated application exceptions.

No AppleScript or UI scripting is used.

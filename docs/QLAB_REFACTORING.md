- `QLabMemoCuePlanMapper` and `QLabNetworkCuePlanMapper` map plan items to `QLabCueCreationRequest`.
- `IQLabOscService` abstracts workspace discovery and session creation; `QLabOscService` is the TCP/OSC implementation.
- `IQLabOscSession` exposes direct QLab commands for one connected workspace; `QLabOscSession` is the transport-backed implementation.
- `QLabProtocol` centralizes QLab OSC command, cue-type, property, and address names.
- `QLabEosNetworkCommand` owns the EOS device-description parameter order and menu values.
- `QLabJsonParser` centralizes reply-field compatibility aliases.

## Why explicit mappers instead of attributes

Attributes work well for static one-to-one metadata. The network-cue mapping also needs runtime data (the selected network patch), optional properties, a dynamic visible parameter stack, ordered parameter writes, and post-write verification. Explicit mapper classes keep those rules visible and independently testable without reflection.

A new plan type now needs:


## Import performance and source selection

- EOS Network cue parameters are written in visible order through `parameterValue/{index}`. Type and command selections change the visible parameter stack, so dependent values are applied only after QLab confirms the preceding value.
- TCP SLIP replies are read through a stateful buffer instead of one asynchronous stream read per byte. This keeps the reliable per-parameter protocol while removing avoidable transport overhead.
- The selected EOS network patch is verified on the first Network cue only; all cues in one import use the same patch ID.
- QLab cue numbers are cleared only for Network cues that will later receive an EOS cue number. Memo cues no longer send redundant number writes.
- The Blazor `InputFile` drop zone is used for both drag-and-drop and click selection. No browser `accept` filter is applied because macOS/WebKit can otherwise disable custom `.esf3d` files; the importer factory still validates the extension after selection.

## v10: secure credentials, patch discovery, and editable preview

- OSC passcodes are kept per QLab workspace in `SessionQLabPasscodeStore` for at most 24 hours and are never written to disk. `MauiSecurePasscodeStore` remains available but is not registered by default; it can be enabled for signed builds that may use the platform keychain.
- The import options query the selected workspace's network patch list and keep the selected patch by its QLab unique ID.
- Preview rows are editable for EOS label, cue notes, and scene text before planning.
- Every preview row has an import checkbox. The master checkbox supports checked, unchecked, and indeterminate states, with explicit Select all and Deselect all actions.
- Manual deselection does not remove a cue from Follow/Hang sequence analysis, so downstream automatic-cue classification remains correct.

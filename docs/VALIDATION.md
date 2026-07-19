# Validation report

Validation performed in the generation environment:

- JSON files parsed successfully.
- XAML, project files, and property lists parsed successfully as XML.
- GitHub Actions workflow parsed successfully as YAML.
- Packaging script passed `bash -n`.
- All C# files passed a lightweight delimiter, string, character, and comment balance scan.
- No Avalonia, `osascript`, or Apple Events entitlement references remain in production source.
- The synthetic `.esf3d` fixture is a valid ZIP archive containing `showdat.dat`.
- The supplied Python parser recovered 3 cues, 3 cue labels, and 3 scene labels from the synthetic fixture.
- The CSV fixture contains 5 cue rows that aggregate into 4 common cues and 1 merged cue-part row.

Not performed in this environment:

- C# compilation, because a .NET SDK was not available in the Linux runtime and external binary installation was
  blocked.
- Mac Catalyst compilation, signing, or notarization, which require macOS and Xcode.
- Live QLab OSC integration, which requires QLab 5 running on macOS.

The repository includes a macOS GitHub Actions workflow and a manual QLab integration test plan for these remaining
checks.

## v10 validation additions

- Plan-builder self-test verifies that deselecting a Follow cue does not break classification of its following cue.
- Static checks verify that network patches are selected by QLab unique ID rather than free-text matching.
- Passcode storage is isolated behind `IQLabPasscodeStore`. The default `SessionQLabPasscodeStore` is process-local and
  expires values after 24 hours. The optional Keychain-backed `MauiSecurePasscodeStore` is present but not registered
  and requires a signed Mac Catalyst runtime test before enabling.
- The tri-state master checkbox uses a small JavaScript bridge because HTML exposes `indeterminate` as a DOM property
  rather than a persistent attribute.

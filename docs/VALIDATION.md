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

- C# compilation, because a .NET SDK was not available in the Linux runtime and external binary installation was blocked.
- Mac Catalyst compilation, signing, or notarization, which require macOS and Xcode.
- Live QLab OSC integration, which requires QLab 5 running on macOS.

The repository includes a macOS GitHub Actions workflow and a manual QLab integration test plan for these remaining checks.

## Summary

Describe what changed and why.

## User-visible behavior

Explain any change a user will notice. Write `None` when the change is internal only.

## Validation

List the commands and manual checks you ran, for example:

```text
dotnet test tests/EosToQLab.Tests/EosToQLab.Tests.csproj -c Release
dotnet build src/EosToQLab.Application/EosToQLab.Application.csproj -f net10.0-maccatalyst
```

## Parser fixtures

For EOS archive parser changes, state which compatibility or regression fixture was added or updated and why. Otherwise write `Not applicable`.

## Safety and compatibility

Describe effects on QLab workspace safety, rollback, OSC behavior, credentials, archive parsing, or supported EOS versions. Otherwise write `No change`.

## Checklist

- [ ] The change is focused and does not include unrelated formatting or refactoring.
- [ ] Tests were added or updated for changed behavior.
- [ ] The complete automated test suite passes.
- [ ] A Mac Catalyst build was run when application or platform files changed.
- [ ] Documentation was updated when behavior or architecture changed.
- [ ] No passcodes, credentials, private show data, build output, or generated user state is included.
- [ ] New EOS fixtures are minimal, artificial, or fully sanitized.
- [ ] I have read and followed `CONTRIBUTING.md` and the Code of Conduct.

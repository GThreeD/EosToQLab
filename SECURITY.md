# Security Policy

## Supported versions

Security fixes are provided for the latest published release. The `main` branch may receive fixes before the next release but should be treated as development code.

Older releases and third-party forks are not supported unless explicitly stated otherwise.

## Reporting a vulnerability

Please do **not** report security vulnerabilities through public GitHub issues, pull requests, or discussions.

Use the repository's **Security** tab and choose **Report a vulnerability** when private vulnerability reporting is available:

https://github.com/GThreeD/EosToQLab/security/advisories/new

If that option is unavailable, contact the maintainer, **@GThreeD**, privately using a contact method listed on the maintainer's GitHub profile. Do not include exploit details in public communication.

A useful report includes:

- the affected EosToQLab version or commit;
- macOS version and CPU architecture;
- a clear description of the issue and its potential impact;
- minimal reproduction steps or a proof of concept;
- whether credentials, QLab passcodes, local files, or network access are involved;
- any suggested mitigation, if known.

Do not attach confidential EOS show files or QLab workspaces. Use a minimal sanitized reproduction instead.

## Response process

The maintainer will try to:

1. acknowledge the report within 7 days;
2. confirm whether the issue is reproducible and in scope;
3. coordinate a fix and release timeline with the reporter;
4. publish an advisory when users need to take action.

Actual response times may vary because this is a small, volunteer-maintained project.

Please allow reasonable time for investigation and remediation before public disclosure.

## Scope

Security reports may include, but are not limited to:

- unintended disclosure of QLab passcodes or other credentials;
- unsafe handling of untrusted CSV, ESF2, or ESF3D files;
- archive traversal, arbitrary file access, or code execution;
- unsafe OSC or TCP behavior that crosses documented trust boundaries;
- unintended modification or deletion of QLab workspace data;
- dependency vulnerabilities with a demonstrated impact on EosToQLab.

General bugs, parser incompatibilities, and feature requests should use the normal issue forms.

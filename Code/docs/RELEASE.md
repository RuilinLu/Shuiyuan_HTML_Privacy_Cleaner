# Release Process

This document is for maintainers preparing GitHub releases.

## Version Naming

Use semantic version tags:

- main release tag: `v7.0.0`
- optional architecture tags if needed: `v7.0.0-win-x64`, `v7.0.0-win-x86`, `v7.0.0-win-arm64`

The recommended public release is one GitHub Release named `v7.0.0` with three EXE assets and checksum files. This is easier for users than three separate release pages.

## Pre-Release Checklist

1. Build three architectures with `cd Code; .\build.ps1`.
2. Confirm `CHECKSUMS-SHA256.txt` was regenerated.
3. Run x64 command-line cleaning on a private local sample.
4. Run x86 command-line cleaning on the same private local sample.
5. Confirm x64 and x86 outputs match where deterministic.
6. Run identity consistency checks on full-anonymous output.
7. Open the full-anonymous output in Edge.
8. Scan text and EXE files for private local strings.
9. Confirm no private sample HTML, reports, screenshots, or `TEST` folders are staged.

## Release Assets

Upload:

- `Code/dist/win-x64/ShuiyuanHtmlPrivacyCleaner.exe`
- `Code/dist/win-x86/ShuiyuanHtmlPrivacyCleaner.exe`
- `Code/dist/win-arm64/ShuiyuanHtmlPrivacyCleaner.exe`
- `CHECKSUMS-SHA256.txt`

Suggested asset names:

- `ShuiyuanHtmlPrivacyCleaner-v7.0.0-win-x64.exe`
- `ShuiyuanHtmlPrivacyCleaner-v7.0.0-win-x86.exe`
- `ShuiyuanHtmlPrivacyCleaner-v7.0.0-win-arm64.exe`
- `CHECKSUMS-SHA256.txt`

## Release Notes Template

```markdown
# Shuiyuan HTML Privacy Cleaner V7

V7 is a self-contained offline Windows release for cleaning SingleFile HTML snapshots of Shuiyuan/Discourse pages.

## What Changed

- Full-anonymous mode preserves the original Discourse/SingleFile structure in place.
- Topic statistics and participant summary rows are preserved, anonymized, and kept horizontal.
- Public users are mapped to stable fictional avatars, display names, and usernames.
- Reply targets, topic-map participants, post headers, and discovered textual mentions use consistent fake identities.
- Watermarks, hidden watermark layers, current-user data, extension remnants, site identity, public user identifiers, badges/flair, hidden preload data, and site outlinks are removed in full-anonymous mode.
- Release EXEs are built without debug symbols to avoid embedding local source paths.

## Which File Should I Download?

- Windows x64: `ShuiyuanHtmlPrivacyCleaner-v7.0.0-win-x64.exe`
- Windows x86: `ShuiyuanHtmlPrivacyCleaner-v7.0.0-win-x86.exe`
- Windows ARM64: `ShuiyuanHtmlPrivacyCleaner-v7.0.0-win-arm64.exe`

Most users should download the x64 EXE. No .NET installation is required.

## Tested

- x64 GUI/CLI path on Windows x64.
- x86 CLI path on Windows x64.
- ARM64 build and checksum generation; runtime validation still needs ARM64 hardware.

## Input Requirement

Use an `.html` snapshot saved by SingleFile. The most-tested capture workflow is Edge + SingleFile on Windows x64.

## Checksums

See `CHECKSUMS-SHA256.txt`.
```

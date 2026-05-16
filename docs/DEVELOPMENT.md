# Development Guide

## Stack

- Language: C#
- UI: Windows Forms
- Runtime target: .NET 9 Windows
- Packaging: self-contained single-file Windows executables
- Helper tooling: PowerShell, optional Node.js for regenerating the anonymous identity pack

The source code lives under `Code/src` in release packages. The main classes are:

- `Program.cs`: GUI/CLI entrypoint
- `MainForm.cs`: WinForms UI
- `ProgressForm.cs`: progress window
- `CleanerEngine.cs`: shared cleaning, reporting, and audit logic
- `CleanerEngine.Anonymous.cs`: full-anonymous Discourse/user anonymization logic
- `AnonymousIdentityPack.cs`: offline identity pack reader
- `Branding.cs`: embedded logo/icon loading

## Build

Install .NET SDK 9 on Windows, then run:

```powershell
cd Code
.\build.ps1
```

This creates:

- `Code/dist/win-x64/ShuiyuanHtmlPrivacyCleaner.exe`
- `Code/dist/win-x86/ShuiyuanHtmlPrivacyCleaner.exe`
- `Code/dist/win-arm64/ShuiyuanHtmlPrivacyCleaner.exe`
- `Code/CHECKSUMS-SHA256.txt`

The build uses:

- `PublishSingleFile=true`
- `SelfContained=true`
- `EnableCompressionInSingleFile=true`
- `DebugType=None`
- `DebugSymbols=false`

Debug symbols are disabled for release builds so local source paths are not embedded into public EXE files.

## Anonymous Identity Pack

The generated pack is stored at:

```text
Code/assets/anonymous_identity_pack.jsonl.gz
```

The generator source is:

```text
Code/tools/avatar_pack_generator
```

The generated pack is not committed to `main` because it is larger than GitHub's normal 100 MB file limit. It is attached to the GitHub Release as an optional asset. If the pack is missing, `Code/build.ps1` can rebuild it with npm. Runtime use is fully offline; the EXE does not download identities or avatars.

## Brand Assets

The app uses:

- EXE icon: `Figure/shuiyuan_symbol_app_icon.svg`
- window/header logo: `Figure/shuiyuan_html_watermark_tool_logo.svg`

`tools/GenerateBrandAssets.ps1` creates `.ico` and `.png` assets used by the Windows build.

## Adding Cleaning Rules

Keep rules scoped and auditable:

1. Add detection in `FillReportAndAudit`.
2. Add removal or rewrite logic in the relevant cleaning stage.
3. Add a post-clean audit item.
4. Validate with full-anonymous and personal modes.
5. Check that output HTML still opens in Edge.

Avoid rules that delete broad containers unless you have verified the HTML structure. V7 intentionally preserves the Discourse topic statistics / participant summary row and anonymizes it instead of deleting it.

## Repository Hygiene

Do not commit private samples. Keep generated private validation files outside the repository. Release EXEs can be committed because this repository is intended to provide direct downloadable offline binaries, but private HTML snapshots and reports must stay local.

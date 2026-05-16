# Validation Guide

This guide describes how to validate a release without publishing private sample files.

## Never Commit Private Samples

Do not commit:

- original saved forum HTML files
- cleaned outputs generated from private samples
- screenshots of private samples
- reports generated from private samples
- local `TEST` folders

Run validation locally and only commit source, docs, build scripts, figures, checksums, and release binaries.

## Command-Line Smoke Test

Use a local private sample file outside the repository:

```powershell
.\dist\win-x64\ShuiyuanHtmlPrivacyCleaner.exe --clean input.html output_full.html --mode full --report report_full.txt --report-style standard --language zh-CN --overwrite
```

Expected:

- exit code is `0`
- output HTML exists
- report exists
- report review checks are zero for full-anonymous privacy categories

## x86 Check on x64 Windows

The x86 executable can run on normal x64 Windows:

```powershell
.\dist\win-x86\ShuiyuanHtmlPrivacyCleaner.exe --clean input.html output_full_x86.html --mode full --report report_x86.txt --report-style standard --language zh-CN --overwrite
```

For deterministic rules, x64 and x86 outputs for the same input should normally have the same SHA256 hash.

## ARM64 Check

The ARM64 executable cannot normally run on x64 Windows. Validate at least:

- file exists
- SHA256 is listed
- release asset is uploaded

Runtime behavior should be checked on ARM64 Windows hardware when available.

## Identity Consistency Check

The repository includes:

```powershell
python .\Code\tools\verify_v7_identity_consistency.py output_full.html
```

Expected:

- `bad_identity 0`
- `bad_avatar 0`
- `bad_post_header_identity 0`
- `bad_reply_identity 0`
- `bad_reply_avatar 0`

## Browser Rendering Check

Open the output HTML in Edge, or use a local headless Edge screenshot.

Check:

- page is not blank
- visible images render
- topic statistics / participant row stays horizontal
- post headers show consistent fake display names and usernames
- reply references point to the same fake identity as the original target user
- no visible watermark remains

## Binary Privacy Scan

Before publishing, scan release EXEs for private local strings such as usernames, local paths, and private sample titles. The V7 build disables debug symbols to avoid embedding local source paths.

Example terms to scan locally:

- private usernames
- private numeric IDs
- private topic titles
- local paths
- validation file names

## GitHub Release Checklist

Before creating a release:

1. `README.md` and `README.html` describe V7 behavior.
2. `RELEASE_NOTES_v7.md` is current.
3. `CHECKSUMS-SHA256.txt` matches the three EXEs.
4. `dist/win-x64`, `dist/win-x86`, and `dist/win-arm64` contain only EXE release files.
5. The repository contains no private test HTML, generated private output, screenshots, or reports.
6. The release notes mention that x64 is the most-tested path, x86 was tested on x64 Windows, and ARM64 still needs hardware validation.


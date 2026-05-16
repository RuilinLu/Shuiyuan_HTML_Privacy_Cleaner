# Contributing

Contributions are welcome, especially:

- new Shuiyuan/Discourse watermark patterns
- safer anonymization rules
- layout fixes for SingleFile snapshots
- documentation improvements
- validation scripts that do not require private samples

## Ground Rules

- Do not commit private forum snapshots.
- Do not commit generated outputs from private snapshots.
- Do not commit screenshots or reports that contain private content.
- Do not commit credentials, cookies, tokens, or browser profiles.
- Keep changes compatible with offline use.

## Development Setup

Use Windows with .NET SDK 9.

```powershell
.\build.ps1
```

The build script publishes self-contained EXEs for x64, x86, and ARM64.

## Testing

Use your own local sample outside the repository. At minimum:

1. Run personal mode.
2. Run full-anonymous mode.
3. Open the output HTML in Edge.
4. Check the report.
5. Run `Code/tools/verify_v7_identity_consistency.py` for full-anonymous outputs when possible.

## Pull Request Expectations

Include:

- what privacy issue or layout issue the change addresses
- what files were touched
- what local validation was run
- whether the change affects personal mode, full-anonymous mode, or both


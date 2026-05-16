# Installation Guide

This project publishes self-contained Windows executables. Normal users do not need to install .NET, Node.js, Python, Git, or any browser automation tool.

## Choose the Right EXE

- Windows x64: use `dist/win-x64/ShuiyuanHtmlPrivacyCleaner.exe`.
- Windows x86: use `dist/win-x86/ShuiyuanHtmlPrivacyCleaner.exe`.
- Windows ARM64: use `dist/win-arm64/ShuiyuanHtmlPrivacyCleaner.exe`.

The most-tested combination is Windows x64 + Edge + SingleFile + `win-x64`.

## Download from GitHub Releases

1. Open the repository Releases page.
2. Download the EXE matching your Windows architecture.
3. Put the EXE anywhere convenient, for example Desktop or a tools folder.
4. Double-click the EXE to launch the UI.

Windows may show a SmartScreen warning because this is a small open-source utility and the EXE is not code-signed. Check the file hash against `CHECKSUMS-SHA256.txt` before running it.

## Verify SHA256

In PowerShell:

```powershell
Get-FileHash .\ShuiyuanHtmlPrivacyCleaner.exe -Algorithm SHA256
```

Compare the result with the matching line in `CHECKSUMS-SHA256.txt`.

## SingleFile Requirement

This tool expects an HTML file saved by the SingleFile browser extension. It does not log in to any website and does not capture web pages by itself.

Recommended capture workflow:

1. Install SingleFile in Edge.
2. Open the target Shuiyuan/Discourse page.
3. Wait until images and replies are fully loaded.
4. Use SingleFile to save the page as one `.html` file.
5. Clean that `.html` file with this tool.

Reference: <https://github.com/RuilinLu/SingleFile>


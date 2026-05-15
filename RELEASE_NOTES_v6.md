# Shuiyuan HTML Privacy Cleaner V6

V6 is the first release intended for public distribution.

## Background

This tool is designed for Shuiyuan/Discourse pages saved as single-file `.html` snapshots by the SingleFile browser extension. It does not process screenshots or live web pages directly.

Recommended and tested workflow:

1. Use Microsoft Edge.
2. Install SingleFile. See <https://github.com/RuilinLu/SingleFile>.
3. Save the current Shuiyuan page as one `.html` file.
4. Run this cleaner on that `.html` file.

The most thoroughly tested combination is Edge + SingleFile + Windows x64 + `ShuiyuanHtmlPrivacyCleaner-win-x64.exe`. The x86 build was command-line tested on Windows x64. The ARM64 build was produced and its PE architecture was verified, but it was not run on a real ARM64 device.

## Download

Download the EXE that matches your Windows device:

- `ShuiyuanHtmlPrivacyCleaner-win-x64.exe`: recommended for most 64-bit Windows PCs.
- `ShuiyuanHtmlPrivacyCleaner-win-x86.exe`: for 32-bit Windows.
- `ShuiyuanHtmlPrivacyCleaner-win-arm64.exe`: for Windows ARM64 devices.

The EXE is self-contained and offline. You do not need to install .NET.

## Modes

- Personal mode removes the current saver/login user's watermark, hidden watermark, `currentUser`, avatar menu, saved timestamp, extension remnants, and manually supplied audit terms.
- Full anonymous mode directly anonymizes the original SingleFile/Discourse HTML in place. It preserves the original forum-like layout as much as possible while removing site branding, external Shuiyuan/SJTU URLs, preloaded JSON, public user identifiers, avatars, user badges, titles, flairs, and links that can identify users or the source site.

## V6 fixes

- Full anonymous mode no longer rebuilds a separate static page from extracted posts.
- Fixed the blank-page bug caused by malformed `<style>` parsing after anonymization.
- Added real percentage progress window.
- Added EXE icon and in-app header logo from the provided SVG assets.
- Added stricter report checks for hidden watermark overlays, CSRF/theme meta data, public user markers, badge/flair nodes, site URLs, and `<style>` tag pairing.
- Added stable `用户1`, `用户2` style labels next to post avatars and in post headers in full anonymous mode.
- Replaced original avatars with generated inline SVG virtual avatars, with different colors per anonymous user number.
- Protected `data:image/...` resources while replacing user aliases, preventing accidental Base64 corruption and broken content images.
- Removed the bottom suggested/related topics area and login notification footer in full anonymous mode to avoid extra site/recommendation traces and static-layout misalignment.

## Validation

Real saved HTML samples were used for validation:

- Real sample A, full anonymous mode: Edge parsed `topic-post=21`, `cooked=21`, page height about `5952px`.
- Real sample B, full anonymous mode: Edge parsed `topic-post=62`, `cooked=62`, page height about `15947px`.
- In full anonymous outputs, saver keywords, site domain/brand traces, public user markers, badge/flair nodes, hidden preload JSON, site external URLs, CSRF/theme meta data, SingleFile timestamp, visible watermark, and hidden watermark all audited as `0`.
- Latest full-anonymous regression sample: all 21 posts have visible anonymous labels, all avatar images are generated inline SVGs, content PNG Base64 data validates cleanly, Edge renders the page and content image, and the suggested/related topics block is absent.
- x64 and x86 builds produced identical full-anonymous HTML for the validation sample.

## Checksums

See `CHECKSUMS-SHA256.txt` in the repository for SHA256 hashes of the three EXE builds.

## Important limitation

No tool can mathematically prove that unknown arbitrary steganography or deliberately hidden data is absent. V6 targets the known Shuiyuan/Discourse SingleFile traces found in the tested files: visible watermarks, hidden watermark layers, login-state data, user identifiers, site links, embedded preloads, browser-extension remnants, and user-supplied audit terms.


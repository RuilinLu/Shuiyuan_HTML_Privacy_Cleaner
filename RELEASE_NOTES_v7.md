# Release Notes V7

V7 focuses on making full-anonymous mode usable as a faithful static Discourse snapshot instead of a simplified rebuilt page.

## Highlights

- Preserves the original SingleFile/Discourse document in place.
- Keeps post content, inline images, emoji, reply blocks, and the reading layout.
- Keeps the Discourse topic statistics / participant summary row, anonymizes its avatars and identifiers, and forces it into a single horizontal row.
- Uses a bundled offline anonymous identity pack for stable fake avatars, display names, and usernames.
- Keeps one real user mapped to one fake identity across post headers, avatar attributes, reply-to tabs, topic-map participants, and textual mentions found by the cleaner.
- Removes watermarks, hidden low-opacity watermark layers, current-user/login traces, extension capture remnants, site identity, public user identifiers, badges/titles/flair, hidden preload data, and site outlinks in full-anonymous mode.
- Provides real percent progress in the UI and three report styles: friendly, standard, and technical.
- Ships self-contained Windows builds for `win-x64`, `win-x86`, and `win-arm64`.

## Tested Locally

- `win-x64` command-line cleaning on sample SingleFile HTML.
- `win-x86` command-line cleaning on x64 Windows.
- Full-anonymous identity consistency verification for post headers, avatars, and reply targets.
- Edge headless rendering of the full-anonymous output to verify that the HTML opens and the topic-map summary row remains horizontal.

ARM64 is built and checksumed, but still needs real ARM64 hardware validation.

## Privacy Boundary

Private sample HTML files, generated sample outputs, screenshots, and local TEST folders must stay outside the public repository and release assets.


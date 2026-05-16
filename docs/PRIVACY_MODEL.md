# Privacy Model and Limits

This document explains what the cleaner tries to remove and what it cannot guarantee.

## Threat Model

The tool is designed for a saved SingleFile HTML snapshot that may contain:

- visible watermarks
- hidden low-opacity watermark overlays
- logged-in account UI
- embedded `currentUser` data
- saver-specific navigation entries
- read/unread state
- SingleFile save timestamps
- browser-extension capture remnants
- public user identifiers
- site domains and brand traces
- hidden Discourse preload JSON
- original image/resource URLs

The tool operates on local HTML text and embedded resources. It does not contact the source site.

## Personal Mode Privacy Goal

Personal mode aims to hide the identity and local environment of the person who saved the page.

It is expected that public forum information remains. For example, public author names, public avatars, public links, and site names can remain in this mode.

## Full-Anonymous Mode Privacy Goal

Full-anonymous mode aims to produce a distributable static reading copy where the source site and real public users are no longer identifiable from the HTML.

The mode attempts to:

- replace public users with stable fictional identities
- remove user profile links, user IDs, user-card attributes, and avatar source URLs
- remove site names, site domains, sidebars, navigation, and activity sections
- remove badges, titles, flair, and group identity hints
- remove hidden preloaded Discourse data
- neutralize source-site links and metadata URLs

## Identity Consistency

Full-anonymous mode builds an alias map from discovered usernames, display names, avatar references, post headers, reply references, and user-card attributes.

For each real user found in one HTML file, the app assigns:

- one fictional display name
- one fictional username
- one fictional avatar

The mapping is stable inside that cleaned file. It is not intended to be stable across unrelated files.

## Embedded Anonymous Identity Pack

The app includes an offline generated identity pack. It uses open-source illustrated avatar generation and fictional names/usernames. The EXE does not download avatar data at runtime.

The project intentionally avoids real-person photo datasets. Real portrait datasets create consent, likeness, and redistribution concerns, which would be inappropriate for a privacy-cleaning tool.

## Preserved Content

The cleaner tries to preserve the readable forum snapshot:

- post body text
- embedded post images
- emoji and reaction-like visible content
- quote/reply blocks
- topic statistics and participant summary row
- the general Discourse reading layout

The topic statistics row is not removed in V7. It is anonymized and kept horizontal.

## Automatic Audit

After cleaning, the app checks for known residue categories, including:

- watermark markers
- current-user/login markers
- SingleFile save timestamp
- Windows paths and `file://`
- site domains and brand terms
- public user identifiers
- user badges/titles/flair
- hidden preload data
- site outlinks/original URLs
- user-supplied extra keywords
- HTML style tag pairing

## Limits

No static cleaner can prove that an arbitrary unknown hidden channel or steganographic payload is impossible. This tool targets known Shuiyuan/Discourse/SingleFile structures and patterns.

Before distributing sensitive files:

1. Use full-anonymous mode.
2. Add your own extra keywords.
3. Read the audit report.
4. Open the output HTML offline and manually inspect visible content.
5. Do not distribute original input files or intermediate test outputs.


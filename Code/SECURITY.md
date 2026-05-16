# Security Policy

## Supported Versions

Only the latest release is actively maintained.

## Reporting a Security or Privacy Issue

Please open a GitHub issue if you find:

- a watermark pattern that is not removed
- a logged-in account trace that remains
- a public user identity leak in full-anonymous mode
- a hidden preload or source-site URL pattern that remains
- a release package containing private test data

Do not include private HTML files directly in public issues. Instead, describe the pattern or create a minimized synthetic sample.

## Secrets and Credentials

Do not put passwords, tokens, cookies, browser profiles, GitHub credentials, or private forum exports into the repository.

The app runs offline and does not need any account credentials.

## Private Samples

Private SingleFile HTML snapshots can contain personal account data, site cookies embedded in metadata, profile links, local paths, or sensitive post content. Keep private samples outside the repository and outside release assets.

## Limitations

This project removes known Shuiyuan/Discourse/SingleFile privacy traces. It cannot prove that arbitrary unknown steganographic data or future site-specific hidden fields are impossible. Review important outputs manually before distribution.


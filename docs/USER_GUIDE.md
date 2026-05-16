# User Guide

## What This App Does

Shuiyuan HTML Privacy Cleaner removes privacy traces from SingleFile HTML snapshots of Shuiyuan/Discourse pages. It works offline and writes a cleaned copy; it does not modify the original input unless you deliberately choose the same output path and allow overwrite.

## Main Screen

### Input HTML

Choose the original SingleFile `.html` file. The file should already contain the saved page resources such as images, avatars, CSS, and emoji.

### Output HTML

Choose where to save the cleaned file. If this box is empty, the app suggests a file name beside the input file:

- personal mode: `_去水印去个人信息.html`
- full-anonymous mode: `_全匿名版.html`

### Review Keywords

Add extra strings that must not remain after cleaning. Use one item per line. Good examples:

- your forum username or display name
- numeric user ID
- avatar file number
- local path fragment
- a suspicious timestamp
- any unique phrase that could identify the saver

The app checks both plain text and URL-encoded forms.

### Mode

`仅清除当前保存者 / 登录者信息` removes saver/login/watermark traces while preserving public forum content.

`全匿名模式` also anonymizes public users, site identity, badges/titles/flair, hidden preload data, and site links.

### Language

The report UI supports Simplified Chinese, Traditional Chinese, and English. This is designed as a list so more languages can be added later.

### Report Style

- Friendly summary: easiest to read.
- Standard report: recommended default.
- Technical details: most useful for debugging.

### Buttons

- `仅分析`: scans the input and writes no output file.
- `清理并审核`: writes the cleaned HTML and immediately audits it.
- `打开输出位置`: opens the folder containing the output file.
- `允许覆盖输出文件`: allows replacing an existing output file.

## Personal Mode

Use this mode when you want the saved HTML to remain a normal public forum snapshot, but you do not want it to reveal who saved it.

It removes:

- SingleFile save timestamp comment
- visible watermark CSS and DOM
- hidden low-opacity watermark overlays
- top-right logged-in avatar/account menu
- `currentUser` account data
- `我的帖子`, `浏览记录`, unread/new tracking entries
- read/unread state markers
- browser extension capture remnants
- extra review keywords
- local Windows paths and `file://` traces

It intentionally keeps:

- public author names and avatars
- public site/category/topic names
- public links and visible post content

## Full-Anonymous Mode

Use this mode when readers should not know the source site or any real public user identity.

It removes or rewrites:

- everything personal mode removes
- site brand and site domain traces
- site sidebar/navigation/activity entries
- public user identifiers and user-card attributes
- user badges, titles, flair, and group markers
- original avatar URLs and user profile links
- hidden Discourse preload data and original resource URLs
- site outlinks and metadata preview URLs

It preserves:

- the original Discourse/SingleFile reading framework as much as possible
- post order and visible post content
- inline images and emoji that are already embedded in the snapshot
- reply blocks
- topic statistics and participant summary row

In full-anonymous mode, one real user maps to one fictional identity throughout the file. The same user should keep the same fake avatar, display name, and username in post headers, reply references, topic-map participants, and textual mentions found by the cleaner.

## Topic Statistics Row

The row that shows values such as `浏览量`, `赞`, `链接`, `用户`, plus a row of participant avatars, is Discourse topic summary content. V7 keeps it. Full-anonymous mode anonymizes the participant avatars and keeps the row horizontal to avoid visual drift.

## After Cleaning

Open the output HTML in Edge and review:

1. The page is not blank.
2. There is no visible watermark layer.
3. The top-right logged-in account menu is gone.
4. In full-anonymous mode, real user names, badges, site name, and site domain do not appear.
5. Images and emoji needed for reading still render.
6. The report review section is zero for the categories that matter to the selected mode.


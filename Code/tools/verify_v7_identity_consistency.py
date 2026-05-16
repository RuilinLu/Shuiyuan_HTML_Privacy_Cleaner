from collections import defaultdict
from pathlib import Path
import hashlib
import re
import sys

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

path = Path(sys.argv[1]) if len(sys.argv) > 1 else Path("v7_sample_full.html")
text = path.read_text(encoding="utf-8", errors="ignore")

triples = re.findall(
    r'data-anon-index="(\d+)"[^>]*data-anon-display="([^"]+)"[^>]*data-anon-username="([^"]+)"',
    text,
)
identities = defaultdict(set)
for index, display, username in triples:
    identities[index].add((display, username))

bad_identity = {index: values for index, values in identities.items() if len(values) > 1}

avatars = defaultdict(set)
for match in re.finditer(r"<img\b[^>]*>", text, re.I):
    tag = match.group(0)
    index_match = re.search(r'data-anon-index="(\d+)"', tag)
    src_match = re.search(r'src="([^"]+)"', tag)
    if index_match and src_match:
        avatars[index_match.group(1)].add(hashlib.sha256(src_match.group(1).encode()).hexdigest()[:12])

bad_avatar = {index: values for index, values in avatars.items() if len(values) > 1}

post_blocks = re.findall(
    r'<div\b(?=[^>]*\btopic-post\b)[\s\S]*?(?=<div\b(?=[^>]*\btopic-post\b)|\Z)',
    text,
    re.I,
)
bad_post_header_identity = []
for pos, block in enumerate(post_blocks, 1):
    header = re.split(r'<div\b[^>]*\bpost__body\b', block, maxsplit=1, flags=re.I)[0]
    name_match = re.search(r'<div\b(?=[^>]*\banonymous-names\b)[^>]*data-anon-index="(\d+)"', header, re.I)
    avatar_div_match = re.search(r'<div\b(?=[^>]*\bpost-avatar\b)[^>]*data-anon-index="(\d+)"', header, re.I)
    avatar_img_match = re.search(r'<img\b(?=[^>]*\bavatar\b)[^>]*data-anon-index="(\d+)"', header, re.I)
    found = [
        ("names", name_match.group(1) if name_match else None),
        ("post-avatar", avatar_div_match.group(1) if avatar_div_match else None),
        ("img.avatar", avatar_img_match.group(1) if avatar_img_match else None),
    ]
    present = [(kind, index) for kind, index in found if index]
    if present:
        unique = {index for _, index in present}
        if len(unique) > 1:
            bad_post_header_identity.append((pos, present))

reply_tabs = re.findall(
    r'<a\b[^>]*\breply-to-tab\b[\s\S]*?</a>',
    text,
    re.I,
)
bad_reply_identity = []
bad_reply_avatar = []
missing_reply_visible = []
for pos, block in enumerate(reply_tabs, 1):
    index_match = re.search(r'data-anon-index="(\d+)"', block)
    display_match = re.search(r'data-anon-display="([^"]+)"', block)
    username_match = re.search(r'data-anon-username="([^"]+)"', block)
    if not (index_match and display_match and username_match):
        bad_reply_identity.append((pos, "missing identity attrs"))
        continue

    index = index_match.group(1)
    display = display_match.group(1)
    username = username_match.group(1)
    if identities.get(index) and (display, username) not in identities[index]:
        bad_reply_identity.append((pos, index, display, username, sorted(identities[index])[:3]))

    img_match = re.search(r'<img\b[^>]*>', block, re.I)
    if img_match:
        src_match = re.search(r'src="([^"]+)"', img_match.group(0))
        if src_match and index in avatars:
            reply_hash = hashlib.sha256(src_match.group(1).encode()).hexdigest()[:12]
            if reply_hash not in avatars[index]:
                bad_reply_avatar.append((pos, index, reply_hash, sorted(avatars[index])[:3]))

    visible = re.sub(r"data:image/[^\"'\s>]+", "", block)
    visible = re.sub(r"<[^>]+>", " ", visible)
    visible = re.sub(r"\s+", " ", visible)
    if display not in visible or ("@" + username) not in visible:
        missing_reply_visible.append((pos, index, visible[:120]))

print("identity_attrs", len(triples))
print("unique_identities", len(identities))
print("bad_identity", len(bad_identity), bad_identity)
print("unique_avatar_indices", len(avatars))
print("bad_avatar", len(bad_avatar), bad_avatar)
print("topic_posts", len(post_blocks))
print("bad_post_header_identity", len(bad_post_header_identity), bad_post_header_identity[:8])
print("reply_tabs", len(reply_tabs))
print("bad_reply_identity", len(bad_reply_identity), bad_reply_identity[:8])
print("bad_reply_avatar", len(bad_reply_avatar), bad_reply_avatar[:8])
print("missing_reply_visible", len(missing_reply_visible), missing_reply_visible[:8])
print("sample", list(identities.items())[:8])

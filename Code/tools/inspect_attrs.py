from pathlib import Path
import re
import sys

path = Path(sys.argv[1])
text = path.read_text(encoding="utf-8", errors="replace")
for pattern in [
    r'<div class="post-avatar"[^>]+>',
    r'<img\b(?=[^>]*\bavatar\b)[^>]+>',
    r'<a\b(?=[^>]*\breply-to-tab\b)[\s\S]*?</a>',
]:
    match = re.search(pattern, text, re.I)
    print("PATTERN", pattern)
    if not match:
        print("missing")
        continue
    block = re.sub(r'data:image/[^"\s>]+', 'DATAURI', match.group(0))
    print(block.encode("unicode_escape").decode("ascii", errors="replace")[:2000])
    print(re.findall(r'data-anon-[a-z-]+="[^"]*"', block))

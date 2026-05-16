from pathlib import Path
import re

text = Path("v7_sample_full.html").read_text(encoding="utf-8", errors="ignore")
checks = [
    "水源活动",
    "水源社区",
    "水源广场",
    "shuiyuan",
    "sjtu.edu.cn",
    "currentUser",
    "watermark-background",
    "data-user-card",
    "data-user-id",
    "/user_avatar/",
    "sidebar-wrapper",
    "sidebar-section",
    "所有类别",
    "所有话题",
    "探索文档话题",
    "https://",
]
print({c: text.lower().count(c.lower()) for c in checks})
print("dataimage", text.count("data:image/"))
print("sfimg", len(re.findall(r"--sf-img-\d+", text)))
print("emoji", len(re.findall("emoji", text, re.I)))
print("img", len(re.findall(r"<img\b", text, re.I)))
print("topic-post", len(re.findall("topic-post", text)))
print("cooked", len(re.findall("cooked", text)))
print("names div", len(re.findall(r"<div[^>]*class=[^>]*names", text, re.I)))
print("first full-name", text.count("first full-name"))
print("second username", text.count("second username"))
print("avatar-name", text.count("anonymous-avatar-name"))
print("data-anon", text.count("data-anon-"))
print("displays", re.findall(r'data-anon-display="([^"]+)', text)[:20])
print("users", re.findall(r'data-anon-username="([^"]+)', text)[:20])
for pat in ["<div id=post", "<article", "class=topic-post", 'class="topic-post', "topic-post clearfix", "data-post-id"]:
    print("pat", pat, text.find(pat), text.count(pat))
idx = text.find("<article")
print("article snippet", text[idx:idx + 3000].encode("unicode_escape").decode() if idx >= 0 else "no article")

"""从 PhoneIconFactory.cs 解析数据，生成手机 UI 风格图标预览 HTML（与 WPF 实现共用同一份图形/配色）。"""
import os
import re

SRC = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..",
                   "src", "TaskbarDock", "Icons", "PhoneIconFactory.cs")
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "phone_preview.html")

text = open(SRC, encoding="utf-8").read()


def parse_glyph_dict(name):
    # 匹配 private static readonly Dictionary<string, string> Name = new() { ... };
    m = re.search(name + r"\s*=\s*new\(\)\s*\{(.*?)\};", text, re.S)
    d = {}
    if not m:
        return d
    body = m.group(1)
    for km in re.finditer(r'\["([^"]+)"\]\s*=\s*"([^"]*)"', body):
        d[km.group(1)] = km.group(2)
    return d


fluent = parse_glyph_dict("FluentGlyphs")
blocks = parse_glyph_dict("BlocksGlyphs")
shared = parse_glyph_dict("SharedGlyphs")


def resolve_glyph(func, key):
    if func == "Fluent":
        return fluent.get(key, "")
    if func == "Blocks":
        return blocks.get(key, "")
    if func == "Shared":
        return shared.get(key, "")
    return ""


# 解析 Map：Map 条目是唯一形如 “= (C(0x..), C(0x..), Word("key"))” 的行
entries = []
hexnum = r"(0x[0-9A-Fa-f]+)"
entry_re = re.compile(
    r'\["([^"]+)"\]\s*=\s*\(C\(' + hexnum + r',\s*' + hexnum + r',\s*' + hexnum +
    r'\),\s*C\(' + hexnum + r',\s*' + hexnum + r',\s*' + hexnum +
    r'\),\s*(\w+)\("([^"]+)"\)\)')
for em in entry_re.finditer(text):
    key = em.group(1)
    c1 = (int(em.group(2), 16), int(em.group(3), 16), int(em.group(4), 16))
    c2 = (int(em.group(5), 16), int(em.group(6), 16), int(em.group(7), 16))
    glyph = resolve_glyph(em.group(8), em.group(9))
    entries.append((key, c1, c2, glyph))

cards = []
for key, c1, c2, glyph in entries:
    c1s = f"rgb({c1[0]},{c1[1]},{c1[2]})"
    c2s = f"rgb({c2[0]},{c2[1]},{c2[2]})"
    gid = "g_" + re.sub(r"\W", "", key)
    cards.append(f"""
    <div class="card">
      <svg viewBox="0 0 100 100" width="92" height="92">
        <defs>
          <linearGradient id="{gid}" x1="0" y1="0" x2="1" y2="1">
            <stop offset="0" stop-color="{c1s}"/>
            <stop offset="1" stop-color="{c2s}"/>
          </linearGradient>
          <linearGradient id="{gid}s" x1="0" y1="0" x2="0" y2="0.55">
            <stop offset="0" stop-color="rgba(255,255,255,0.24)"/>
            <stop offset="1" stop-color="rgba(255,255,255,0)"/>
          </linearGradient>
        </defs>
        <rect x="4" y="4" width="92" height="92" rx="22" fill="url(#{gid})"/>
        <rect x="4" y="4" width="92" height="92" rx="22" fill="url(#{gid}s)"/>
        <g transform="translate(8,8) scale(0.84)">
          <path d="{glyph}" fill="#ffffff" transform="translate(0,0)"/>
        </g>
      </svg>
      <div class="label">{key}</div>
    </div>""")

html = f"""<!doctype html><html lang="zh-CN"><head><meta charset="utf-8">
<title>Phone 风格图标预览</title>
<style>
  body {{ font-family: -apple-system, "Segoe UI", system-ui, sans-serif; background:#f3f3f3; margin:0; padding:24px; color:#1b1b1b; }}
  h1 {{ font-size:18px; font-weight:600; }}
  .sub {{ color:#5c5c5c; font-size:13px; margin-bottom:18px; }}
  .grid {{ display:flex; flex-wrap:wrap; gap:14px; }}
  .card {{ width:120px; text-align:center; background:#fff; border-radius:12px; padding:12px 8px; box-shadow:0 6px 20px rgba(0,0,0,.10); }}
  .label {{ font-size:12px; color:#5c5c5c; margin-top:8px; }}
</style></head><body>
<h1>Phone（彩色应用图标）预览</h1>
<div class="sub">圆角渐变方块 + 白色实心图形，与现有线稿/单色图标完全不同。共 {len(cards)} 个。</div>
<div class="grid">{''.join(cards)}</div>
</body></html>"""

open(OUT, "w", encoding="utf-8").write(html)
print("wrote", OUT, "with", len(cards), "icons")

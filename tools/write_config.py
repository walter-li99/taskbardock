"""生成冒烟测试用的配置文件（指向桌面「程序图标」文件夹）。"""
import json
import os

cfg_dir = os.path.join(os.environ["LOCALAPPDATA"], "TaskbarDock")
os.makedirs(cfg_dir, exist_ok=True)
desktop_icons = os.path.join(os.environ["USERPROFILE"], "Desktop", "程序图标")
cfg = {
    "Groups": [
        {
            "Id": "g1",
            "Name": "常用工具",
            "Folder": desktop_icons,
            "IconPack": "system",
            "IconKey": "folder",
            "CustomIconPath": "",
            "Order": [],
            "Aliases": {},
            "Hidden": [],
            "SortByCustom": True,
        }
    ],
    "ThemeMode": "Light",
    "UseAcrylic": True,
    "MatchTaskbarColor": True,
    "Align": "Left",
    "IconSize": 40,
    "MenuWidth": 300,
    "MenuMaxHeight": 520,
    "ShowItemIcons": True,
    "ItemIconSize": 20,
    "CornerRadius": 8,
    "StartWithWindows": False,
    "ShowTrayIcon": True,
    "Language": "zh-CN",
}
p = os.path.join(cfg_dir, "config.json")
with open(p, "w", encoding="utf-8") as f:
    json.dump(cfg, f, ensure_ascii=False, indent=2)
print("wrote", p)

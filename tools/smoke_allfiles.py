"""验证「显示所有文件」：混合文件夹里各种文件都应出现在菜单里。"""
import json
import os
import shutil
import subprocess
import sys
import time

EXE = r"C:\Users\Ainuc\WorkBuddy\任务栏整合\dist\TaskbarDock.exe"
CFG = os.path.join(os.environ["LOCALAPPDATA"], "TaskbarDock", "config.json")
LOG = os.path.join(os.environ["LOCALAPPDATA"], "TaskbarDock", "log.txt")

TESTDIR = os.path.join(os.environ["USERPROFILE"], "Documents", "TaskbarDock", "混合测试")
if os.path.isdir(TESTDIR):
    shutil.rmtree(TESTDIR)
os.makedirs(os.path.join(TESTDIR, "子文件夹"))

# 各种普通文件 + 一个快捷方式 + 一个隐藏文件（应被跳过）
open(os.path.join(TESTDIR, "说明.txt"), "w", encoding="utf-8").write("hello")
open(os.path.join(TESTDIR, "报告.pdf"), "wb").write(b"%PDF-1.4 fake")
open(os.path.join(TESTDIR, "notes.md"), "w", encoding="utf-8").write("# t")
open(os.path.join(TESTDIR, "图片.png"), "wb").write(b"\x89PNG\r\n\x1a\n" + b"\0" * 64)
open(os.path.join(TESTDIR, "数据.xlsx"), "wb").write(b"fake")
shutil.copy(r"C:\Users\Ainuc\WorkBuddy\任务栏整合\test-shortcuts\记事本.lnk",
            os.path.join(TESTDIR, "记事本.lnk"))
p = os.path.join(TESTDIR, "desktop.ini")
open(p, "w", encoding="utf-8").write("[.ShellClassInfo]")
import ctypes
ctypes.windll.kernel32.SetFileAttributesW(p, 0x2)  # 隐藏

print("目录内容:", sorted(os.listdir(TESTDIR)))


def write_cfg(show_all, sub):
    cfg = {
        "Groups": [{
            "Id": "g1", "Name": "混合", "Folder": TESTDIR, "IconPack": "badge",
            "IconKey": "folder", "CustomIconPath": "", "Order": [], "Aliases": {},
            "Hidden": [], "SortByCustom": True
        }],
        "ThemeMode": "Light", "UseAcrylic": True, "MatchTaskbarColor": True, "Align": "Left",
        "IconSize": 40, "MenuWidth": 300, "MenuMaxHeight": 520, "ShowItemIcons": True,
        "ItemIconSize": 20, "CornerRadius": 8, "StartWithWindows": False,
        "ShowTrayIcon": True, "ShowAllFiles": show_all, "IncludeSubfolders": sub,
        "Language": "zh-CN",
    }
    with open(CFG, "w", encoding="utf-8") as f:
        json.dump(cfg, f, ensure_ascii=False, indent=2)


def restart_and_open():
    subprocess.run(["taskkill", "/IM", "TaskbarDock.exe", "/F"], capture_output=True)
    time.sleep(1)
    subprocess.Popen([EXE, "--startup"])
    time.sleep(4)
    subprocess.run([EXE, "--menu", "g1"], timeout=60)
    time.sleep(2)
    with open(LOG, encoding="utf-8", errors="replace") as f:
        lines = [l for l in f.readlines() if "展开菜单" in l]
    return lines[-1].strip() if lines else "(no log)"


for show_all, sub in [(True, True), (True, False), (False, False)]:
    write_cfg(show_all, sub)
    print("ShowAllFiles=%s IncludeSubfolders=%s -> %s" % (show_all, sub, restart_and_open()))

subprocess.run(["taskkill", "/IM", "TaskbarDock.exe", "/F"], capture_output=True)
print("done")

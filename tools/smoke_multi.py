"""多图标冒烟测试：3 个分组 → 任务栏 3 个按钮，各自弹出自己的菜单。"""
import ctypes
import os
import subprocess
import sys
import time
from ctypes import wintypes

ROOT = r"C:\Users\Ainuc\WorkBuddy\任务栏整合"
EXE = os.path.join(ROOT, "dist", "TaskbarDock.exe")
LOG = os.path.join(os.environ["LOCALAPPDATA"], "TaskbarDock", "log.txt")
OUT = os.path.join(ROOT, "tools", "shots")

WM_SYSCOMMAND = 0x0112
SC_RESTORE = 0xF120

try:
    ctypes.WinDLL("shcore").SetProcessDpiAwareness(2)
except Exception:
    try:
        ctypes.WinDLL("user32").SetProcessDPIAware()
    except Exception:
        pass

u = ctypes.WinDLL("user32", use_last_error=True)

GROUPS = [("g1", "常用工具"), ("g2", "系统工具"), ("g3", "空分组")]


def enum_windows():
    CB = ctypes.WINFUNCTYPE(ctypes.c_bool, wintypes.HWND, wintypes.LPARAM)
    out = []

    @CB
    def cb(hwnd, _):
        cls = ctypes.create_unicode_buffer(256)
        u.GetClassNameW(hwnd, cls, 256)
        ttl = ctypes.create_unicode_buffer(256)
        u.GetWindowTextW(hwnd, ttl, 256)
        out.append((int(hwnd), cls.value, ttl.value))
        return True

    u.EnumWindows(cb, 0)
    return out


def our_windows():
    return [(h, c, t) for (h, c, t) in enum_windows() if "HwndWrapper[TaskbarDock" in c]


def menu_titles():
    return [t for (_, _, t) in our_windows() if t.startswith("MenuWindow")]


def button_rect(app_id):
    ps = os.path.join(ROOT, "tools", "find_taskbar_button.ps1")
    r = subprocess.run(["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", ps,
                        "-AppId", app_id], capture_output=True, timeout=120)
    lines = [l.strip() for l in r.stdout.decode("utf-8", "replace").splitlines() if l.strip()]
    if not lines or lines[0] == "none":
        return None
    parts = lines[0].split()
    return tuple(int(p) for p in parts[:4])


def log_tail(n=12):
    try:
        with open(LOG, encoding="utf-8", errors="replace") as f:
            return f.readlines()[-n:]
    except Exception:
        return []


def main():
    # 结束可能残留的旧进程
    subprocess.run(["taskkill", "/IM", "TaskbarDock.exe", "/F"], capture_output=True)
    time.sleep(1)

    proc = subprocess.Popen([EXE, "--startup"], cwd=os.path.dirname(EXE))
    time.sleep(5)

    print("=== 1. 分组窗口 ===")
    for h, c, t in our_windows():
        print("  hwnd=%s title=%r" % (h, t))

    print("=== 2. 任务栏按钮（按 AppId 定位） ===")
    rects = {}
    for gid, name in GROUPS:
        r = button_rect("TaskbarDock.Group." + gid)
        rects[gid] = r
        print("  %s %-6s -> %s" % (gid, name, r))

    print("=== 3. 用 SC_RESTORE 触发第 2 个图标 ===")
    target = next((h for (h, c, t) in our_windows() if t == "系统工具"), None)
    print("  系统工具 hwnd =", target)
    if target:
        u.PostMessageW(target, WM_SYSCOMMAND, SC_RESTORE, 0)
        time.sleep(1.5)
        print("  menus:", menu_titles())

    # 关掉菜单
    subprocess.run(["taskkill", "/IM", "TaskbarDock.exe", "/F"], capture_output=True)
    time.sleep(1)
    proc = subprocess.Popen([EXE, "--startup"], cwd=os.path.dirname(EXE))
    time.sleep(5)

    print("=== 4. 第二个实例 --menu g3（走 WM_COPYDATA 转发） ===")
    subprocess.run([EXE, "--menu", "g3"], cwd=os.path.dirname(EXE), timeout=60)
    time.sleep(2.0)
    print("  menus:", menu_titles())
    print("  --- log ---")
    for line in log_tail(8):
        print("   ", line.rstrip())

    print("=== 5. 第二个实例 --menu g1 ===")
    subprocess.run([EXE, "--menu", "g1"], cwd=os.path.dirname(EXE), timeout=60)
    time.sleep(2.0)
    print("  menus:", menu_titles())

    print("=== 6. 空分组菜单（占位项） ===")
    subprocess.run([EXE, "--menu", "g3"], cwd=os.path.dirname(EXE), timeout=60)
    time.sleep(2.0)
    print("  menus:", menu_titles())

    time.sleep(0.5)
    subprocess.run(["taskkill", "/IM", "TaskbarDock.exe", "/F"], capture_output=True)
    print("done")


if __name__ == "__main__":
    sys.exit(main())

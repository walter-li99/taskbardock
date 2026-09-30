import json, os, subprocess, time, sys, ctypes
from ctypes import wintypes
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as st

EXE = st.EXE
CFG = os.path.join(os.environ["LOCALAPPDATA"], "TaskbarDock", "config.json")
ICO = os.path.join(os.environ["LOCALAPPDATA"], "TaskbarDock", "Icons", "g1.ico")

u = ctypes.WinDLL("user32", use_last_error=True)
WM = u.RegisterWindowMessageW("TaskbarDock.Command.v1")
CMD_REFRESH = 4
BTN = (2416, 2088, 66, 72)

def set_icon(key, pack=None):
    with open(CFG, "r", encoding="utf-8") as f:
        cfg = json.load(f)
    cfg["Groups"][0]["IconKey"] = key
    if pack: cfg["Groups"][0]["IconPack"] = pack
    with open(CFG, "w", encoding="utf-8") as f:
        json.dump(cfg, f, ensure_ascii=False, indent=2)

def mtime():
    try: return os.path.getmtime(ICO)
    except OSError: return None

def post_refresh_to_pid(pid):
    res = []
    CB = ctypes.WINFUNCTYPE(ctypes.c_bool, wintypes.HWND, wintypes.LPARAM)
    @CB
    def cb(hwnd, l):
        p = wintypes.DWORD()
        u.GetWindowThreadProcessId(hwnd, ctypes.byref(p))
        if p.value == pid:
            u.PostMessageW(hwnd, WM, CMD_REFRESH, 0)
            res.append(int(hwnd))
        return True
    u.EnumWindows(cb, 0)
    return res

proc = subprocess.Popen([EXE, "--startup"], cwd=os.path.dirname(EXE))
time.sleep(4.5)

# 1) 不改图标，refresh：ICO 不应重建
post_refresh_to_pid(proc.pid)
time.sleep(1.5)
m1 = mtime()
post_refresh_to_pid(proc.pid)
time.sleep(1.5)
m2 = mtime()
print("no-change refresh: ico rebuilt =", (m1 != m2), "(False 正确)")

# 2) 改图标（globe），refresh：ICO 应重建、任务栏按钮应变成新图标
set_icon("globe")
posts = post_refresh_to_pid(proc.pid)
print("posted to hwnds:", posts)
time.sleep(2.5)
m3 = mtime()
print("icon changed: ico rebuilt =", (m3 != m2), "(True 正确)")
x, y, w, h = BTN
st.save("_icon_globe.png", x, y, w, h)

# 3) 换成 Badge (彩色徽章) 包
set_icon("star", "badge")
post_refresh_to_pid(proc.pid)
time.sleep(2.5)
st.save("_icon_badge.png", x, y, w, h)

proc.terminate()
try: proc.wait(timeout=5)
except Exception: proc.kill()
print("done")

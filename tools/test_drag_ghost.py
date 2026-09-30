import os, subprocess, time, sys, ctypes
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as st

EXE = st.EXE
u = st.u
g = st.g

proc = subprocess.Popen([EXE, "--startup"], cwd=os.path.dirname(EXE))
time.sleep(4.5)

# 点击任务栏按钮打开菜单（按钮位置同冒烟测试）
x, y, w, h = (2416, 2088, 66, 72)
cx, cy = x + w / 2, y + h / 2
st.click(cx, cy)
time.sleep(1.5)

# 菜单矩形（冒烟测试测得）：(2260, 1778, 2638, 2079)
# 在菜单中部某项上按下并拖动（菜单物理矩形约 2260,1778 - 2638,2079）
sx, sy = 2400, 1900
u.SetCursorPos(int(sx), int(sy))
time.sleep(0.2)
u.mouse_event(0x0002, 0, 0, 0, 0)  # down
time.sleep(0.15)

# 分步移动
for i in range(1, 9):
    nx = sx + i * 6
    ny = sy + i * 12
    u.SetCursorPos(int(nx), int(ny))
    time.sleep(0.05)

time.sleep(0.4)
cur = ctypes.wintypes.POINT()
ctypes.windll.user32.GetCursorPos(ctypes.byref(cur))
print("cursor at", cur.x, cur.y)

# 枚举本进程所有可见窗口，找跟随框（小窗口且靠近光标）
res = []
CB = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.wintypes.HWND, ctypes.wintypes.LPARAM)
@CB
def cb(hwnd, l):
    p = ctypes.wintypes.DWORD()
    u.GetWindowThreadProcessId(hwnd, ctypes.byref(p))
    if p.value != proc.pid: return True
    r = ctypes.wintypes.RECT()
    u.GetWindowRect(hwnd, ctypes.byref(r))
    if u.IsWindowVisible(hwnd) and (r.right - r.left) > 4:
        res.append((int(hwnd), r.left, r.top, r.right - r.left, r.bottom - r.top))
    return True
u.EnumWindows(cb, 0)
for r in sorted(res, key=lambda t: abs(t[1] - cur.x) + abs(t[2] - cur.y))[:4]:
    d = abs(r[1] - cur.x) + abs(r[2] - cur.y)
    print("win", r, "dist-to-cursor", d)

st.save("_drag_ghost.png", cur.x - 60, cur.y - 60, 460, 220)

u.mouse_event(0x0004, 0, 0, 0, 0)  # up
time.sleep(0.5)

proc.terminate()
try: proc.wait(timeout=5)
except Exception: proc.kill()
print("done")

"""冒烟测试：启动程序 → 截图任务栏 → 点击 dock 图标 → 截图菜单 → 退出。"""
import ctypes
import os
import struct
import subprocess
import sys
import time
import zlib
from ctypes import wintypes

EXE = r"C:\Users\Ainuc\WorkBuddy\任务栏整合\dist\TaskbarDock.exe"
OUT = r"C:\Users\Ainuc\WorkBuddy\任务栏整合\tools\shots"

try:
    ctypes.WinDLL("shcore").SetProcessDpiAwareness(2)
except Exception:
    try:
        ctypes.WinDLL("user32").SetProcessDPIAware()
    except Exception:
        pass

u = ctypes.WinDLL("user32", use_last_error=True)
g = ctypes.WinDLL("gdi32", use_last_error=True)
s = ctypes.WinDLL("shell32", use_last_error=True)


class RECT(ctypes.Structure):
    _fields_ = [("left", ctypes.c_long), ("top", ctypes.c_long),
                ("right", ctypes.c_long), ("bottom", ctypes.c_long)]


class APPBARDATA(ctypes.Structure):
    _fields_ = [("cbSize", wintypes.DWORD), ("hWnd", wintypes.HWND),
                ("uCallbackMessage", wintypes.UINT), ("uEdge", wintypes.UINT),
                ("rc", RECT), ("lParam", ctypes.c_longlong)]


def taskbar():
    a = APPBARDATA()
    a.cbSize = ctypes.sizeof(APPBARDATA)
    s.SHAppBarMessage(5, ctypes.byref(a))
    return a.rc.left, a.rc.top, a.rc.right, a.rc.bottom, a.uEdge


def windows():
    EnumWindowsProc = ctypes.WINFUNCTYPE(ctypes.c_bool, wintypes.HWND, ctypes.POINTER(ctypes.c_int))
    out = []

    def cb(hwnd, _):
        r = RECT()
        u.GetWindowRect(hwnd, ctypes.byref(r))
        cls = ctypes.create_unicode_buffer(256)
        u.GetClassNameW(hwnd, cls, 256)
        ttl = ctypes.create_unicode_buffer(256)
        u.GetWindowTextW(hwnd, ttl, 256)
        if u.IsWindowVisible(hwnd) and (r.right - r.left) > 0:
            out.append((hwnd, cls.value, ttl.value, (r.left, r.top, r.right, r.bottom)))
        return True

    u.EnumWindows(EnumWindowsProc(cb), ctypes.byref(ctypes.c_int(0)))
    return out


class BITMAPINFOHEADER(ctypes.Structure):
    _fields_ = [("biSize", wintypes.DWORD), ("biWidth", ctypes.c_long), ("biHeight", ctypes.c_long),
                ("biPlanes", wintypes.WORD), ("biBitCount", wintypes.WORD), ("biCompression", wintypes.DWORD),
                ("biSizeImage", wintypes.DWORD), ("biXPelsPerMeter", ctypes.c_long),
                ("biYPelsPerMeter", ctypes.c_long), ("biClrUsed", wintypes.DWORD),
                ("biClrImportant", wintypes.DWORD)]


def capture(x, y, w, h):
    hdc = u.GetDC(0)
    mem = g.CreateCompatibleDC(hdc)
    bmp = g.CreateCompatibleBitmap(hdc, w, h)
    old = g.SelectObject(mem, bmp)
    g.BitBlt(mem, 0, 0, w, h, hdc, x, y, 0x00CC0020)
    bi = BITMAPINFOHEADER()
    bi.biSize = ctypes.sizeof(BITMAPINFOHEADER)
    bi.biWidth = w
    bi.biHeight = -h
    bi.biPlanes = 1
    bi.biBitCount = 32
    bi.biCompression = 0
    buf = ctypes.create_string_buffer(w * h * 4)
    g.GetDIBits(mem, bmp, 0, h, buf, ctypes.byref(bi), 0)
    g.SelectObject(mem, old)
    g.DeleteObject(bmp)
    g.DeleteDC(mem)
    u.ReleaseDC(0, hdc)
    raw = buf.raw
    rgba = bytearray(w * h * 4)
    for i in range(w * h):
        b0, g0, r0, a0 = raw[i * 4], raw[i * 4 + 1], raw[i * 4 + 2], raw[i * 4 + 3]
        rgba[i * 4] = r0
        rgba[i * 4 + 1] = g0
        rgba[i * 4 + 2] = b0
        rgba[i * 4 + 3] = 255
    return bytes(rgba)


def png(w, h, data):
    raw = bytearray()
    stride = w * 4
    for yy in range(h):
        raw.append(0)
        raw += data[yy * stride:(yy + 1) * stride]

    def chunk(tag, payload):
        return (struct.pack(">I", len(payload)) + tag + payload
                + struct.pack(">I", zlib.crc32(tag + payload) & 0xFFFFFFFF))

    ihdr = struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)
    return (b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr)
            + chunk(b"IDAT", zlib.compress(bytes(raw), 6)) + chunk(b"IEND", b""))


def save(name, x, y, w, h):
    os.makedirs(OUT, exist_ok=True)
    p = os.path.join(OUT, name)
    with open(p, "wb") as f:
        f.write(png(w, h, capture(x, y, w, h)))
    print("saved", p, f"{w}x{h}")


def click(x, y):
    u.SetCursorPos(int(x), int(y))
    time.sleep(0.15)
    u.mouse_event(0x0002, 0, 0, 0, 0)
    time.sleep(0.08)
    u.mouse_event(0x0004, 0, 0, 0, 0)


def main():
    tb = taskbar()
    print("taskbar", tb)
    sw = u.GetSystemMetrics(0)
    sh = u.GetSystemMetrics(1)
    print("screen", sw, sh)

    proc = subprocess.Popen([EXE], cwd=os.path.dirname(EXE))
    time.sleep(4.5)

    wins = windows()
    docks = [w for w in wins if "HwndWrapper" in w[1] and "TaskbarDock" in w[1]]
    for d in docks:
        print("dock:", d[3], d[1][:60], d[2])
    menus = [w for w in wins if "MenuWindow" in w[2] or "MenuWindow" in w[1]]
    print("menus before:", len(menus))

    cap_h = min(600, sh - 40)
    save("01_dock.png", 0, sh - cap_h, min(760, sw), cap_h)

    if docks:
        l, t, r, b = docks[0][3]
        cx, cy = (l + r) / 2, (t + b) / 2
        print("click dock at", cx, cy)
        click(cx, cy)
        time.sleep(1.5)
        wins2 = windows()
        menus = [w for w in wins2 if "Menu" in w[2] or "Menu" in w[1]]
        print("menus after:", [(m[2], m[3]) for m in menus])
        save("02_menu.png", 0, sh - cap_h, min(760, sw), cap_h)

    time.sleep(0.5)
    proc.terminate()
    try:
        proc.wait(timeout=5)
    except Exception:
        proc.kill()
    print("done")


if __name__ == "__main__":
    sys.exit(main())

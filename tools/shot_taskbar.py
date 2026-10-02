"""截取任务栏（或指定区域）并存成 PNG。用法：python tools/shot_taskbar.py [x w] [输出文件名]"""
import ctypes
import os
import struct
import sys
import zlib
from ctypes import wintypes

try:
    ctypes.WinDLL("shcore").SetProcessDpiAwareness(2)
except Exception:
    try:
        ctypes.WinDLL("user32").SetProcessDPIAware()
    except Exception:
        pass

u = ctypes.WinDLL("user32")
g = ctypes.WinDLL("gdi32")
s = ctypes.WinDLL("shell32")


class RECT(ctypes.Structure):
    _fields_ = [("l", ctypes.c_long), ("t", ctypes.c_long),
                ("r", ctypes.c_long), ("b", ctypes.c_long)]


class APPBARDATA(ctypes.Structure):
    _fields_ = [("cbSize", wintypes.DWORD), ("hWnd", wintypes.HWND),
                ("uCallbackMessage", wintypes.UINT), ("uEdge", wintypes.UINT),
                ("rc", RECT), ("lParam", ctypes.c_longlong)]


class BIH(ctypes.Structure):
    _fields_ = [("biSize", wintypes.DWORD), ("biWidth", ctypes.c_long), ("biHeight", ctypes.c_long),
                ("biPlanes", wintypes.WORD), ("biBitCount", wintypes.WORD), ("biCompression", wintypes.DWORD),
                ("biSizeImage", wintypes.DWORD), ("biXPelsPerMeter", ctypes.c_long),
                ("biYPelsPerMeter", ctypes.c_long), ("biClrUsed", wintypes.DWORD),
                ("biClrImportant", wintypes.DWORD)]


def taskbar_rect():
    a = APPBARDATA()
    a.cbSize = ctypes.sizeof(APPBARDATA)
    s.SHAppBarMessage(5, ctypes.byref(a))
    return a.rc.l, a.rc.t, a.rc.r - a.rc.l, a.rc.b - a.rc.t


def capture(x, y, w, h):
    hdc = u.GetDC(0)
    mem = g.CreateCompatibleDC(hdc)
    bmp = g.CreateCompatibleBitmap(hdc, w, h)
    old = g.SelectObject(mem, bmp)
    g.BitBlt(mem, 0, 0, w, h, hdc, x, y, 0x00CC0020)
    bi = BIH()
    bi.biSize = ctypes.sizeof(BIH)
    bi.biWidth = w
    bi.biHeight = -h
    bi.biPlanes = 1
    bi.biBitCount = 32
    buf = ctypes.create_string_buffer(w * h * 4)
    g.GetDIBits(mem, bmp, 0, h, buf, ctypes.byref(bi), 0)
    g.SelectObject(mem, old)
    g.DeleteObject(bmp)
    g.DeleteDC(mem)
    u.ReleaseDC(0, hdc)

    raw = buf.raw
    rgba = bytearray(w * h * 4)
    for i in range(w * h):
        b0, g0, r0 = raw[i * 4], raw[i * 4 + 1], raw[i * 4 + 2]
        rgba[i * 4] = r0
        rgba[i * 4 + 1] = g0
        rgba[i * 4 + 2] = b0
        rgba[i * 4 + 3] = 255

    data = bytearray()
    for yy in range(h):
        data.append(0)
        data += rgba[yy * w * 4:(yy + 1) * w * 4]

    def chunk(tag, payload):
        return (struct.pack(">I", len(payload)) + tag + payload
                + struct.pack(">I", zlib.crc32(tag + payload) & 0xFFFFFFFF))

    return (b"\x89PNG\r\n\x1a\n"
            + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
            + chunk(b"IDAT", zlib.compress(bytes(data), 6))
            + chunk(b"IEND", b""))


def main():
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    out = os.path.join(root, "tools", "shots")
    os.makedirs(out, exist_ok=True)

    tx, ty, tw, th = taskbar_rect()
    print("taskbar", tx, ty, tw, th)

    x, w = tx, tw
    name = "taskbar.png"
    if len(sys.argv) > 2:
        x, w = int(sys.argv[1]), int(sys.argv[2])
    if len(sys.argv) > 3:
        name = sys.argv[3]

    p = os.path.join(out, name)
    with open(p, "wb") as f:
        f.write(capture(x, ty, w, th))
    print("saved", p, f"{w}x{th}")


if __name__ == "__main__":
    sys.exit(main())

"""检查任务栏图标方向：把运行中窗口的 HICON 和生成的 ICO 与自定义原图逐像素比对。

用法：python tools/verify_icon_orientation.py
依赖：Pillow；需要 TaskbarDock 正在运行（用于读取 WM_GETICON 的 HICON）。
判定依据用 alpha 通道：HICON 的颜色是预乘的，RGB 不宜直接和原图比。
任一产物上下颠倒时退出码为 1。
"""

import ctypes as C
import io
import json
import os
import struct
import sys
from ctypes import wintypes as W

from PIL import Image, ImageChops, ImageStat

u = C.windll.user32
g = C.windll.gdi32
u.SendMessageW.restype = C.c_void_p
u.SendMessageW.argtypes = [W.HWND, C.c_uint, C.c_void_p, C.c_void_p]
u.GetDC.restype = C.c_void_p
u.GetIconInfo.argtypes = [C.c_void_p, C.c_void_p]
u.GetIconInfo.restype = C.c_int


class ICONINFO(C.Structure):
    _fields_ = [
        ("fIcon", C.c_int), ("xHotspot", C.c_uint), ("yHotspot", C.c_uint),
        ("hbmMask", C.c_void_p), ("hbmColor", C.c_void_p),
    ]


class BMIH(C.Structure):
    _fields_ = [
        ("biSize", C.c_uint), ("biWidth", C.c_int), ("biHeight", C.c_int),
        ("biPlanes", C.c_ushort), ("biBitCount", C.c_ushort),
        ("biCompression", C.c_uint), ("biSizeImage", C.c_uint),
        ("biXPelsPerMeter", C.c_int), ("biYPelsPerMeter", C.c_int),
        ("biClrUsed", C.c_uint), ("biClrImportant", C.c_uint),
    ]


class BMI(C.Structure):
    _fields_ = [("bmiHeader", BMIH), ("bmiColors", C.c_uint * 3)]


class BITMAP(C.Structure):
    _fields_ = [
        ("bmType", C.c_long), ("bmWidth", C.c_long), ("bmHeight", C.c_long),
        ("bmWidthBytes", C.c_long), ("bmPlanes", C.c_ushort),
        ("bmBitsPixel", C.c_ushort), ("bmBits", C.c_void_p),
    ]


g.GetObjectW.argtypes = [C.c_void_p, C.c_int, C.c_void_p]
g.GetDIBits.argtypes = [C.c_void_p, C.c_void_p, C.c_uint, C.c_uint,
                        C.c_void_p, C.POINTER(BMI), C.c_uint]

BASE = os.path.join(os.environ["LOCALAPPDATA"], "TaskbarDock")
WM_GETICON, ICON_BIG = 0x007F, 1


def mad(a, b):
    """两张单通道图的平均绝对差，越小越接近。"""
    return ImageStat.Stat(ImageChops.difference(a, b)).mean[0]


def orientation(alpha, source):
    """返回 (正立误差, 翻转误差, 是否正立)。"""
    ref = source.resize(alpha.size, Image.LANCZOS).getchannel("A")
    up = mad(alpha, ref)
    down = mad(alpha, ref.transpose(Image.FLIP_TOP_BOTTOM))
    return up, down, up < down


def list_windows():
    found = {}
    CB = C.WINFUNCTYPE(C.c_bool, W.HWND, W.LPARAM)

    def cb(hwnd, _):
        buf = C.create_unicode_buffer(256)
        u.GetWindowTextW(hwnd, buf, 256)
        if buf.value:
            found.setdefault(buf.value, hwnd)
        return True

    u.EnumWindows(CB(cb), 0)
    return found


def hicon_image(hicon):
    info = ICONINFO()
    if not u.GetIconInfo(C.c_void_p(hicon), C.byref(info)) or not info.hbmColor:
        return None
    bm = BITMAP()
    g.GetObjectW(C.c_void_p(info.hbmColor), C.sizeof(BITMAP), C.byref(bm))
    w, h = bm.bmWidth, abs(bm.bmHeight)
    bmi = BMI()
    hd = bmi.bmiHeader
    hd.biSize = C.sizeof(BMIH)
    hd.biWidth, hd.biHeight = w, -h  # 负高度 = 自顶向下
    hd.biPlanes, hd.biBitCount, hd.biCompression = 1, 32, 0
    buf = (C.c_ubyte * (w * h * 4))()
    dc = u.GetDC(0)
    ok = g.GetDIBits(C.c_void_p(dc), C.c_void_p(info.hbmColor), 0, h,
                     C.byref(buf), C.byref(bmi), 0)
    u.ReleaseDC(0, C.c_void_p(dc))
    g.DeleteObject(C.c_void_p(info.hbmColor))
    g.DeleteObject(C.c_void_p(info.hbmMask))
    return Image.frombytes("RGBA", (w, h), bytes(buf), "raw", "BGRA") if ok else None


def ico_image(path):
    raw = open(path, "rb").read()
    off, size = struct.unpack_from("<II", raw, 18)  # 第一个 ICONDIRENTRY 的数据偏移/长度
    return Image.open(io.BytesIO(raw[off:off + size])).convert("RGBA")


def main():
    cfg = json.load(open(os.path.join(BASE, "config.json"), encoding="utf-8"))
    windows = list_windows()
    bad = 0
    print("分组        产物        正立误差  翻转误差  结论")
    for group in cfg["Groups"]:
        custom = group.get("CustomIconPath") or ""
        if not os.path.isfile(custom):
            continue
        source = Image.open(custom).convert("RGBA")
        name = group["Name"]

        hwnd = windows.get(name)
        if hwnd:
            hicon = u.SendMessageW(hwnd, WM_GETICON, C.c_void_p(ICON_BIG), C.c_void_p(0))
            img = hicon_image(hicon) if hicon else None
            if img is None:
                print(f"{name:<10}  HICON      (读取失败)")
            else:
                up, down, ok = orientation(img.getchannel("A"), source)
                bad += 0 if ok else 1
                print(f"{name:<10}  HICON      {up:8.2f} {down:9.2f}  {'正立 [OK]' if ok else '倒立 [FAIL]'}")

        path = os.path.join(BASE, "Icons", group["Id"] + ".ico")
        if os.path.isfile(path):
            up, down, ok = orientation(ico_image(path).getchannel("A"), source)
            bad += 0 if ok else 1
            print(f"{name:<10}  ICO文件    {up:8.2f} {down:9.2f}  {'正立 [OK]' if ok else '倒立 [FAIL]'}")

    print("\n结论:", "全部正立" if bad == 0 else f"{bad} 项倒立")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())

"""诊断分组窗口的图标签名：窗口类/标题/矩形 + AppID + RelaunchIconResource + WM_GETICON 句柄。"""
import ctypes
import os
import subprocess
from ctypes import wintypes

u = ctypes.WinDLL("user32", use_last_error=True)
shell = ctypes.WinDLL("shell32")

class GUID(ctypes.Structure):
    _fields_ = [("d1", ctypes.c_uint32), ("d2", ctypes.c_uint16),
                ("d3", ctypes.c_uint16), ("d4", ctypes.c_ubyte * 8)]

def mkguid(s):
    import uuid
    x = uuid.UUID(s)
    g = GUID()
    g.d1, g.d2, g.d3 = x.time_low, x.time_mid, x.time_hi_version
    g.d4 = (ctypes.c_ubyte * 8)(*x.bytes[8:])
    return g

class PROPERTYKEY(ctypes.Structure):
    _fields_ = [("fmtid", GUID), ("pid", ctypes.c_uint32)]

class PROPVARIANT(ctypes.Structure):
    _fields_ = [("vt", ctypes.c_uint16), ("r1", ctypes.c_uint16), ("r2", ctypes.c_uint16),
                ("r3", ctypes.c_uint16), ("p", ctypes.c_void_p), ("p2", ctypes.c_void_p)]

IID_PS = mkguid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")
AUM = "9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"
SHGetPropertyStoreForWindow = shell.SHGetPropertyStoreForWindow
SHGetPropertyStoreForWindow.argtypes = [wintypes.HWND, ctypes.POINTER(GUID), ctypes.POINTER(ctypes.c_void_p)]
SHGetPropertyStoreForWindow.restype = ctypes.c_long

def get_prop(hwnd, pid):
    pps = ctypes.c_void_p()
    hr = SHGetPropertyStoreForWindow(hwnd, ctypes.byref(IID_PS), ctypes.byref(pps))
    if hr != 0 or not pps:
        return "<store hr=%d>" % hr
    try:
        vtbl = ctypes.cast(pps, ctypes.POINTER(ctypes.POINTER(ctypes.c_void_p))).contents
        GetValue = ctypes.WINFUNCTYPE(ctypes.c_long, ctypes.c_void_p, ctypes.POINTER(PROPERTYKEY), ctypes.POINTER(PROPVARIANT))(vtbl[5])
        Release = ctypes.WINFUNCTYPE(ctypes.c_ulong, ctypes.c_void_p)(vtbl[2])
        key = PROPERTYKEY(mkguid(AUM), pid)
        pv = PROPVARIANT()
        hr = GetValue(pps, ctypes.byref(key), ctypes.byref(pv))
        if hr != 0:
            return "<get hr=%d>" % hr
        if pv.vt == 0x1F:
            return ctypes.wstring_at(pv.p)
        return "<vt=%#x>" % pv.vt
    finally:
        pass

EnumWindowsProc = ctypes.WINFUNCTYPE(ctypes.c_bool, wintypes.HWND, wintypes.LPARAM)
u.EnumWindows.argtypes = [EnumWindowsProc, wintypes.LPARAM]

pids = set()
for line in subprocess.check_output(["tasklist", "/FI", "IMAGENAME eq TaskbarDock.exe"],
                                    text=True, encoding="gbk", errors="ignore").splitlines():
    p = line.split()
    if len(p) > 1 and p[1].isdigit():
        pids.add(int(p[1]))

class RECT(ctypes.Structure):
    _fields_ = [("l", ctypes.c_long), ("t", ctypes.c_long), ("r", ctypes.c_long), ("b", ctypes.c_long)]

WM_GETICON = 0x7F
rows = []

def cb(hwnd, _):
    pid = wintypes.DWORD()
    u.GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
    if pid.value not in pids:
        return True
    title = ctypes.create_unicode_buffer(512)
    u.GetWindowTextW(hwnd, title, 512)
    cls = ctypes.create_unicode_buffer(256)
    u.GetClassNameW(hwnd, cls, 256)
    r = RECT()
    u.GetWindowRect(hwnd, ctypes.byref(r))
    small = u.SendMessageW(hwnd, WM_GETICON, 0, 0)
    big = u.SendMessageW(hwnd, WM_GETICON, 1, 0)
    small2 = u.SendMessageW(hwnd, WM_GETICON, 2, 0)
    rows.append((hwnd, title.value, cls.value, (r.l, r.t, r.r - r.l, r.b - r.t), small, big, small2))
    return True

u.EnumWindows(EnumWindowsProc(cb), 0)

for hwnd, title, cls, rect, small, big, small2 in rows:
    print("=" * 78)
    print("hwnd=%#010x cls=%s" % (hwnd, cls))
    print("  title=%r rect=%s" % (title, rect))
    print("  AppID       = %r" % get_prop(hwnd, 5))
    print("  RelaunchIcon= %r" % get_prop(hwnd, 8))
    print("  WM_GETICON  small=%#x big=%#x small2=%#x" % (small or 0, big or 0, small2 or 0))

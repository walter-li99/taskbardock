"""诊断：列出指定进程（或全部 TaskbarDock）窗口的位置/尺寸，并与任务栏矩形对比。"""
import ctypes
import sys
from ctypes import wintypes

u = ctypes.WinDLL("user32", use_last_error=True)
s = ctypes.WinDLL("shell32", use_last_error=True)

RECT = wintypes.RECT


class APPBARDATA(ctypes.Structure):
    _fields_ = [("cbSize", wintypes.DWORD), ("hWnd", wintypes.HWND),
                ("uCallbackMessage", wintypes.UINT), ("uEdge", wintypes.UINT),
                ("rc", RECT), ("lParam", ctypes.c_longlong)]


def taskbar_rect():
    a = APPBARDATA()
    a.cbSize = ctypes.sizeof(APPBARDATA)
    s.SHAppBarMessage(5, ctypes.byref(a))  # ABM_GETTASKBARPOS
    return a.rc.left, a.rc.top, a.rc.right, a.rc.bottom, a.uEdge


EnumWindowsProc = ctypes.WINFUNCTYPE(ctypes.c_bool, wintypes.HWND, ctypes.POINTER(ctypes.c_int))
u.EnumWindows.argtypes = [EnumWindowsProc, ctypes.POINTER(ctypes.c_int)]


def windows_of_pid(pid):
    out = []

    def cb(hwnd, _):
        p = ctypes.c_ulong()
        u.GetWindowThreadProcessId(hwnd, ctypes.byref(p))
        if p.value != pid:
            return True
        r = RECT()
        u.GetWindowRect(hwnd, ctypes.byref(r))
        buf = ctypes.create_unicode_buffer(256)
        u.GetClassName(hwnd, buf, 256)
        title_buf = ctypes.create_unicode_buffer(256)
        u.GetWindowText(hwnd, title_buf, 256)
        out.append({
            "hwnd": hwnd,
            "class": buf.value,
            "title": title_buf.value,
            "rect": (r.left, r.top, r.right, r.bottom),
            "visible": bool(u.IsWindowVisible(hwnd)),
            "style_ex": u.GetWindowLongW(hwnd, -20),
        })
        return True

    u.EnumWindows(EnumWindowsProc(cb), ctypes.byref(ctypes.c_int(0)))
    return out


def main():
    pid = None
    if len(sys.argv) > 1:
        pid = int(sys.argv[1])
    if pid is None:
        import subprocess
        out = subprocess.run(["tasklist", "/FI", "IMAGENAME eq TaskbarDock.exe", "/NH"],
                             capture_output=True, text=True, errors="ignore")
        for line in out.stdout.splitlines():
            if "TaskbarDock" in line:
                for tok in line.split():
                    if tok.isdigit():
                        pid = int(tok)
                        break
            if pid:
                break
    if pid is None:
        print("没有运行中的 TaskbarDock")
        return

    tb = taskbar_rect()
    edge_names = {0: "LEFT", 1: "TOP", 2: "RIGHT", 3: "BOTTOM"}
    print(f"任务栏 rect={tb[0:4]} edge={edge_names.get(tb[4], tb[4])}")

    ws = windows_of_pid(pid)
    if not ws:
        print("没有找到窗口")
    for w in ws:
        l, t, r, b = w["rect"]
        print(f"hwnd={w['hwnd']} vis={w['visible']} ex=0x{w['style_ex']:08X} "
              f"rect=({l},{t})-({r},{b}) {r-l}x{b-t} class={w['class'][:48]} title={w['title']!r}")


if __name__ == "__main__":
    main()

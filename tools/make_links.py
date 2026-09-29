"""生成测试用快捷方式（.lnk），用 IShellLink COM，不依赖 WScript.Shell。"""
import ctypes
import os
import sys
from ctypes import wintypes

ole32 = ctypes.WinDLL("ole32", use_last_error=True)


class GUID(ctypes.Structure):
    _fields_ = [("Data1", ctypes.c_ulong), ("Data2", ctypes.c_ushort),
                ("Data3", ctypes.c_ushort), ("Data4", ctypes.c_ubyte * 8)]

    @classmethod
    def from_str(cls, s):
        s = s.strip("{}")
        a, b, c, d, e = s.split("-")
        g = cls(int(a, 16), int(b, 16), int(c, 16), (ctypes.c_ubyte * 8)(0))
        tail = bytes.fromhex(d + e)
        for i in range(8):
            g.Data4[i] = tail[i]
        return g


CLSID_ShellLink = GUID.from_str("{00021401-0000-0000-C000-000000000046}")
IID_IShellLinkW = GUID.from_str("{000214F9-0000-0000-C000-000000000046}")
IID_IPersistFile = GUID.from_str("{0000010b-0000-0000-C000-000000000046}")

VT = ctypes.POINTER(ctypes.c_void_p)


def vtable(ptr, index, restype, argtypes):
    vt = ctypes.cast(ptr, ctypes.POINTER(VT)).contents
    fn = ctypes.WINFUNCTYPE(restype, *argtypes)(vt[index])
    return fn


def make_lnk(path, target, description=""):
    p = ctypes.c_void_p()
    ole32.CoCreateInstance(ctypes.byref(CLSID_ShellLink), None, 1,
                           ctypes.byref(IID_IShellLinkW), ctypes.byref(p))
    if not p:
        raise RuntimeError("CoCreateInstance 失败")

    set_path = vtable(p, 20, ctypes.HRESULT, [ctypes.c_void_p, ctypes.c_wchar_p])
    set_desc = vtable(p, 7, ctypes.HRESULT, [ctypes.c_void_p, ctypes.c_wchar_p])
    set_path(p, target)
    if description:
        set_desc(p, description)

    pf = ctypes.c_void_p()
    qi = vtable(p, 0, ctypes.HRESULT,
                [ctypes.c_void_p, ctypes.POINTER(GUID), ctypes.POINTER(ctypes.c_void_p)])
    qi(p, ctypes.byref(IID_IPersistFile), ctypes.byref(pf))
    save = vtable(pf, 6, ctypes.HRESULT, [ctypes.c_void_p, ctypes.c_wchar_p, ctypes.c_bool])
    hr = save(pf, path, True)
    if hr != 0:
        raise RuntimeError(f"保存失败 hr=0x{hr & 0xFFFFFFFF:08X}")


def main():
    folder = sys.argv[1] if len(sys.argv) > 1 else \
        r"C:\Users\Ainuc\WorkBuddy\任务栏整合\test-shortcuts"
    os.makedirs(folder, exist_ok=True)
    items = [
        ("记事本", r"C:\Windows\System32\notepad.exe"),
        ("计算器", r"C:\Windows\System32\calc.exe"),
        ("画图", r"C:\Windows\System32\mspaint.exe"),
        ("终端 Windows Terminal", r"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe"),
        ("资源管理器", r"C:\Windows\explorer.exe"),
        ("截图工具", r"C:\Windows\System32\SnippingTool.exe"),
    ]
    ole32.CoInitialize(None)
    for name, target in items:
        p = os.path.join(folder, name + ".lnk")
        if os.path.exists(target):
            make_lnk(p, target, name)
            print("created", p)
        else:
            print("skip (missing target)", name, target)


if __name__ == "__main__":
    main()

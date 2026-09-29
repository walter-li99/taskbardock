using System;
using System.Runtime.InteropServices;
using static TaskbarDock.Native.Win32;

namespace TaskbarDock.Native;

/// <summary>读写窗口的 Shell 属性（AppUserModelID 等），决定任务栏按钮的分组与固定行为。</summary>
public static class WindowProperties
{
    private static readonly Guid IidPropertyStore = new("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");
    private static Guid IidPropertyStoreLocal => IidPropertyStore;

    // System.AppUserModel.*
    private static readonly Guid AppUserModel = new("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");
    public static PROPERTYKEY Id => new(AppUserModel, 5);
    public static PROPERTYKEY RelaunchCommand => new(AppUserModel, 2);
    public static PROPERTYKEY RelaunchDisplayName => new(AppUserModel, 4);
    public static PROPERTYKEY RelaunchIcon => new(AppUserModel, 8);

    public static void Set(IntPtr hwnd, PROPERTYKEY key, string value)
    {
        if (hwnd == IntPtr.Zero || string.IsNullOrEmpty(value)) return;
        try
        {
            var iid = IidPropertyStoreLocal;
            int hr = SHGetPropertyStoreForWindow(hwnd, ref iid, out var p);
            if (hr != 0 || p == IntPtr.Zero) return;
            try
            {
                var store = (IPropertyStore)Marshal.GetObjectForIUnknown(p);
                var pv = new PropVariant { vt = VT_LPWSTR, pwszVal = Marshal.StringToHGlobalUni(value) };
                try { store.SetValue(ref key, ref pv); store.Commit(); }
                finally { Marshal.FreeHGlobal(pv.pwszVal); }
            }
            finally { Marshal.Release(p); }
        }
        catch (Exception ex)
        {
            Services.ConfigService.Log("设置窗口属性失败: " + ex.Message);
        }
    }
}

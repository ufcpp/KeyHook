using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

Console.WriteLine("hook keyboard events...");

using (hookKeyDown(k =>
{
    if (k.Shift) Console.Write("Shift + ");
    if (k.Ctrl) Console.Write("Ctrl + ");
    if (k.Alt) Console.Write("Alt + ");
    Console.WriteLine($"{(char)k.Code} ({k.Code})");
}))
{
    wait();
}

static SafeHandle hookKeyDown(Action<KeyDownArgs> callback)
{
    static string moduleName()
    {
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule!;
        return module.ModuleName!;
    }

    SafeHandle hHook = null!;

    hHook = PInvoke.SetWindowsHookEx(WINDOWS_HOOK_ID.WH_KEYBOARD_LL,
        (code, wParam, lParam) =>
        {
            var keyMessage = (nuint)wParam;

            // WM_KEYDOWN, WM_SYSKEYDOWN のみ
            if (keyMessage is not (0x100 or 0x104)) goto END;

            var key = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

            // shift, ctrl, alt 除外
            // VK_SHIFT, VK_CONTROL, VK_MENU, VK_LSHIFT, VK_RSHIFT, VK_LCONTROL, VK_RCONTROL, VK_LMENU, VK_RMENU
            if (key.vkCode is 0x10 or 0x11 or 0x12 or 0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA4 or 0xA5) goto END;

            // GetKeyState の8ビット目が1のとき、「押されてる」。
            // VK_SHIFT, VK_CONTROL, VK_MENU (alt)
            callback(new(
                key.vkCode,
                (PInvoke.GetKeyState(0x10) & 0x80) != 0,
                (PInvoke.GetKeyState(0x11) & 0x80) != 0,
                (PInvoke.GetKeyState(0x12) & 0x80) != 0
                ));

            END:
            return PInvoke.CallNextHookEx(hHook, code, wParam, lParam);
        },
        PInvoke.GetModuleHandle(moduleName()),
        0);

    return hHook;
}

static void wait()
{
    while (PInvoke.GetMessage(out var message, default, 0, 0))
    {
        PInvoke.TranslateMessage(message);
        PInvoke.DispatchMessage(message);
    }
}

record struct KeyDownArgs(uint Code, bool Shift, bool Ctrl, bool Alt);

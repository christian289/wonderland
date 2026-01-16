using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Wonderland.WpfServices;

/// <summary>
/// 전역 키보드 훅 (포커스 없이도 키 입력 감지)
/// Global keyboard hook (detect key input without focus)
/// </summary>
public sealed class GlobalKeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private IntPtr _hookId = IntPtr.Zero;
    private readonly LowLevelKeyboardProc _proc;
    private bool _isHooked;

    /// <summary>
    /// 키 입력 이벤트
    /// Key pressed event
    /// </summary>
    public event EventHandler<Key>? KeyPressed;

    public GlobalKeyboardHook()
    {
        _proc = HookCallback;
    }

    /// <summary>
    /// 훅 시작
    /// Start hook
    /// </summary>
    public void Start()
    {
        if (_isHooked) return;

        _hookId = SetHook(_proc);
        _isHooked = true;
    }

    /// <summary>
    /// 훅 중지
    /// Stop hook
    /// </summary>
    public void Stop()
    {
        if (!_isHooked) return;

        UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
        _isHooked = false;
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName!), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == WM_KEYDOWN)
        {
            var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var key = KeyInterop.KeyFromVirtualKey((int)hookStruct.vkCode);
            KeyPressed?.Invoke(this, key);
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        Stop();
    }
}

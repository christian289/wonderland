using System.Runtime.InteropServices;
using System.Windows;

namespace Wonderland.WpfServices;

/// <summary>
/// 전역 마우스 훅 (Viewer 모드에서 마우스 위치 추적용)
/// </summary>
public sealed class GlobalMouseHook : IDisposable
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_MOUSEMOVE = 0x0200;

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    private IntPtr _hookId = IntPtr.Zero;
    private readonly LowLevelMouseProc _proc;
    private bool _isHooked;

    /// <summary>
    /// 마우스 이동 이벤트
    /// </summary>
    public event EventHandler<System.Windows.Point>? MouseMoved;

    public GlobalMouseHook()
    {
        _proc = HookCallback;
    }

    /// <summary>
    /// 훅 시작
    /// </summary>
    public void Start()
    {
        if (_isHooked) return;

        _hookId = SetHook(_proc);
        _isHooked = true;
    }

    /// <summary>
    /// 훅 중지
    /// </summary>
    public void Stop()
    {
        if (!_isHooked) return;

        UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
        _isHooked = false;
    }

    private static IntPtr SetHook(LowLevelMouseProc proc)
    {
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName!), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == WM_MOUSEMOVE)
        {
            var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            MouseMoved?.Invoke(this, new System.Windows.Point(hookStruct.pt.x, hookStruct.pt.y));
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        Stop();
    }
}

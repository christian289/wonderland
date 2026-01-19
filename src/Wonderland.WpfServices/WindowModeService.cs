using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Wonderland.Domain.Enums;

namespace Wonderland.WpfServices;

/// <summary>
/// 창 모드 서비스 (Viewer/Edit 전환)
/// </summary>
public sealed class WindowModeService
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtr64(hWnd, nIndex)
            : GetWindowLongPtr32(hWnd, nIndex);
    }

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
            : SetWindowLongPtr32(hWnd, nIndex, dwNewLong);
    }

    private IntPtr _hwnd;
    private AppMode _currentMode = AppMode.Viewer;

    /// <summary>
    /// 현재 모드
    /// </summary>
    public AppMode CurrentMode => _currentMode;

    /// <summary>
    /// 모드 변경 이벤트
    /// </summary>
    public event EventHandler<AppMode>? ModeChanged;

    /// <summary>
    /// 창 핸들로 초기화
    /// </summary>
    public void Initialize(Window window)
    {
        _hwnd = new WindowInteropHelper(window).Handle;

        // 초기: Viewer 모드
        EnableClickThrough();
    }

    /// <summary>
    /// 모드 설정
    /// </summary>
    public void SetMode(AppMode mode)
    {
        if (_currentMode == mode) return;

        _currentMode = mode;

        if (mode == AppMode.Viewer)
        {
            EnableClickThrough();
        }
        else
        {
            DisableClickThrough();
        }

        ModeChanged?.Invoke(this, mode);
    }

    /// <summary>
    /// 모드 토글
    /// </summary>
    public void ToggleMode()
    {
        SetMode(_currentMode == AppMode.Viewer ? AppMode.Edit : AppMode.Viewer);
    }

    /// <summary>
    /// Viewer 모드: 마우스 클릭이 창을 통과
    /// </summary>
    private void EnableClickThrough()
    {
        if (_hwnd == IntPtr.Zero) return;

        var exStyle = GetWindowLongPtr(_hwnd, GWL_EXSTYLE).ToInt64();
        SetWindowLongPtr(_hwnd, GWL_EXSTYLE, new IntPtr(exStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED));
    }

    /// <summary>
    /// Edit 모드: 정상적인 마우스 입력
    /// </summary>
    private void DisableClickThrough()
    {
        if (_hwnd == IntPtr.Zero) return;

        var exStyle = GetWindowLongPtr(_hwnd, GWL_EXSTYLE).ToInt64();
        SetWindowLongPtr(_hwnd, GWL_EXSTYLE, new IntPtr((exStyle & ~WS_EX_TRANSPARENT) | WS_EX_LAYERED));
    }
}

using System.Drawing;
using System.Windows.Forms;
using Wonderland.Domain.Enums;

namespace Wonderland.WpfServices;

/// <summary>
/// 시스템 트레이 아이콘 서비스
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private ToolStripMenuItem? _modeMenuItem;
    private AppMode _currentMode = AppMode.Viewer;

    /// <summary>
    /// 종료 요청 이벤트
    /// </summary>
    public event EventHandler? ExitRequested;

    /// <summary>
    /// 모드 전환 요청 이벤트
    /// </summary>
    public event EventHandler? ModeToggleRequested;

    /// <summary>
    /// Edit 모드 요청 이벤트
    /// </summary>
    public event EventHandler? EditModeRequested;

    /// <summary>
    /// 트레이 아이콘 초기화
    /// </summary>
    public void Initialize()
    {
        _contextMenu = new ContextMenuStrip();

        // 모드 전환 메뉴
        _modeMenuItem = new ToolStripMenuItem("Switch to Edit Mode (F12)");
        _modeMenuItem.Click += (s, e) => ModeToggleRequested?.Invoke(this, EventArgs.Empty);
        _contextMenu.Items.Add(_modeMenuItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // 종료 메뉴
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
        _contextMenu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = CreateDefaultIcon(),
            Visible = true,
            Text = "Wonderland",
            ContextMenuStrip = _contextMenu
        };

        // 더블클릭으로 Edit 모드 전환
        _notifyIcon.DoubleClick += (s, e) => EditModeRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 현재 모드 업데이트
    /// </summary>
    public void UpdateMode(AppMode mode)
    {
        _currentMode = mode;

        if (_modeMenuItem is not null)
        {
            _modeMenuItem.Text = mode == AppMode.Viewer
                ? "Switch to Edit Mode (F12)"
                : "Switch to Viewer Mode (F12)";
        }

        if (_notifyIcon is not null)
        {
            _notifyIcon.Text = mode == AppMode.Viewer
                ? "Wonderland (Viewer)"
                : "Wonderland (Edit)";
        }
    }

    /// <summary>
    /// 기본 아이콘 생성
    /// </summary>
    private static Icon CreateDefaultIcon()
    {
        // 간단한 16x16 아이콘 생성
        using var bitmap = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bitmap);

        // 배경: 투명
        g.Clear(Color.Transparent);

        // 원 그리기 (눈송이 느낌)
        using var brush = new SolidBrush(Color.FromArgb(100, 180, 220));
        g.FillEllipse(brush, 2, 2, 12, 12);

        using var whiteBrush = new SolidBrush(Color.White);
        g.FillEllipse(whiteBrush, 5, 5, 6, 6);

        // Icon.FromHandle로 생성된 아이콘은 Clone()으로 복사하여 핸들 해제
        var hIcon = bitmap.GetHicon();
        using var tempIcon = Icon.FromHandle(hIcon);
        var clonedIcon = (Icon)tempIcon.Clone();
        DestroyIcon(hIcon);
        return clonedIcon;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    /// <summary>
    /// 풍선 알림 표시
    /// </summary>
    public void ShowNotification(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon?.ShowBalloonTip(3000, title, text, icon);
    }

    public void Dispose()
    {
        if (_notifyIcon is not null)
        {
            // 아이콘을 숨긴 후 Dispose (트레이에 잔상 방지)
            _notifyIcon.Visible = false;
            _notifyIcon.Icon?.Dispose();
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _contextMenu?.Dispose();
        _contextMenu = null;
    }
}

using System.Drawing;
using System.Windows.Forms;
using Wonderland.Domain.Enums;

namespace Wonderland.WpfServices;

/// <summary>
/// 시스템 트레이 아이콘 서비스
/// System tray icon service
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private ToolStripMenuItem? _modeMenuItem;
    private AppMode _currentMode = AppMode.Viewer;

    /// <summary>
    /// 종료 요청 이벤트
    /// Exit requested event
    /// </summary>
    public event EventHandler? ExitRequested;

    /// <summary>
    /// 모드 전환 요청 이벤트
    /// Mode toggle requested event
    /// </summary>
    public event EventHandler? ModeToggleRequested;

    /// <summary>
    /// Edit 모드 요청 이벤트
    /// Edit mode requested event
    /// </summary>
    public event EventHandler? EditModeRequested;

    /// <summary>
    /// 트레이 아이콘 초기화
    /// Initialize tray icon
    /// </summary>
    public void Initialize()
    {
        _contextMenu = new ContextMenuStrip();

        // 모드 전환 메뉴
        // Mode toggle menu
        _modeMenuItem = new ToolStripMenuItem("Switch to Edit Mode (F12)");
        _modeMenuItem.Click += (s, e) => ModeToggleRequested?.Invoke(this, EventArgs.Empty);
        _contextMenu.Items.Add(_modeMenuItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // 종료 메뉴
        // Exit menu
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
        // Double-click to switch to Edit mode
        _notifyIcon.DoubleClick += (s, e) => EditModeRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 현재 모드 업데이트
    /// Update current mode
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
    /// Create default icon
    /// </summary>
    private static Icon CreateDefaultIcon()
    {
        // 간단한 16x16 아이콘 생성
        // Create a simple 16x16 icon
        using var bitmap = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bitmap);

        // 배경: 투명
        // Background: transparent
        g.Clear(Color.Transparent);

        // 원 그리기 (눈송이 느낌)
        // Draw circles (snowflake feel)
        using var brush = new SolidBrush(Color.FromArgb(100, 180, 220));
        g.FillEllipse(brush, 2, 2, 12, 12);

        using var whiteBrush = new SolidBrush(Color.White);
        g.FillEllipse(whiteBrush, 5, 5, 6, 6);

        return Icon.FromHandle(bitmap.GetHicon());
    }

    /// <summary>
    /// 풍선 알림 표시
    /// Show balloon notification
    /// </summary>
    public void ShowNotification(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon?.ShowBalloonTip(3000, title, text, icon);
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
        _contextMenu?.Dispose();
    }
}

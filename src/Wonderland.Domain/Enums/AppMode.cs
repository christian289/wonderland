namespace Wonderland.Domain.Enums;

/// <summary>
/// 앱 모드 정의
/// Application mode definition
/// </summary>
public enum AppMode
{
    /// <summary>
    /// 뷰어 모드: 마우스 입력이 창을 통과
    /// Viewer mode: Mouse input passes through the window
    /// </summary>
    Viewer,

    /// <summary>
    /// 편집 모드: 레이어 위치 및 설정 편집 가능
    /// Edit mode: Layer position and settings can be edited
    /// </summary>
    Edit
}

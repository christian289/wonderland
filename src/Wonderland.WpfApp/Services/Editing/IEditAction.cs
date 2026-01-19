namespace Wonderland.WpfApp.Services.Editing;

/// <summary>
/// 편집 동작 인터페이스
/// </summary>
public interface IEditAction
{
    /// <summary>
    /// 동작 되돌리기
    /// </summary>
    void Undo();
}

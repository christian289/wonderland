namespace Wonderland.WpfApp.Services.Editing;

/// <summary>
/// Undo 기능을 관리하는 서비스
/// </summary>
public sealed class UndoService
{
    private readonly Stack<IEditAction> _undoStack = new();
    private const int MaxUndoCount = 50;

    /// <summary>
    /// Undo 가능 여부
    /// </summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>
    /// Undo 스택 크기
    /// </summary>
    public int Count => _undoStack.Count;

    /// <summary>
    /// 액션 추가
    /// </summary>
    public void Push(IEditAction action)
    {
        _undoStack.Push(action);
        TrimStack();
    }

    /// <summary>
    /// Undo 실행
    /// </summary>
    /// <returns>Undo 성공 여부</returns>
    public bool Undo()
    {
        if (_undoStack.Count == 0)
            return false;

        var action = _undoStack.Pop();
        action.Undo();
        return true;
    }

    /// <summary>
    /// 스택 초기화
    /// </summary>
    public void Clear() => _undoStack.Clear();

    /// <summary>
    /// 스택 크기 제한
    /// </summary>
    private void TrimStack()
    {
        if (_undoStack.Count <= MaxUndoCount)
            return;

        var tempStack = new Stack<IEditAction>();
        for (var i = 0; i < MaxUndoCount; i++)
        {
            tempStack.Push(_undoStack.Pop());
        }

        _undoStack.Clear();

        while (tempStack.Count > 0)
        {
            _undoStack.Push(tempStack.Pop());
        }
    }
}

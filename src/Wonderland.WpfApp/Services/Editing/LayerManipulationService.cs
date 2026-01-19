using System.Windows;
using Wonderland.UI.Controls;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;

namespace Wonderland.WpfApp.Services.Editing;

/// <summary>
/// 레이어 조작 서비스
/// 선택, 드래그, 리사이즈, 회전 로직을 관리
/// </summary>
public sealed class LayerManipulationService
{
    private readonly ParallaxCanvas _canvas;
    private readonly UndoService _undoService;

    /// <summary>
    /// 선택된 레이어 ID
    /// </summary>
    public Guid? SelectedLayerId { get; private set; }

    /// <summary>
    /// 레이어가 선택되었는지 여부
    /// </summary>
    public bool HasSelection => SelectedLayerId.HasValue;

    /// <summary>
    /// 드래그 중인지 여부
    /// </summary>
    public bool IsDragging { get; private set; }

    /// <summary>
    /// 리사이즈 중인지 여부
    /// </summary>
    public bool IsResizing { get; private set; }

    /// <summary>
    /// 회전 중인지 여부
    /// </summary>
    public bool IsRotating { get; private set; }

    /// <summary>
    /// 현재 리사이즈 방향
    /// </summary>
    public ResizeDirection CurrentResizeDirection { get; private set; }

    // 조작 시작 시 저장되는 값들
    private Point _manipulationStartPoint;
    private Rect _originalBounds;
    private double _originalRotation;
    private Point _rotationCenter;
    private double _rotationStartAngle;

    // 상수
    private const double MinSize = 20;

    public LayerManipulationService(ParallaxCanvas canvas, UndoService undoService)
    {
        _canvas = canvas;
        _undoService = undoService;
    }

    /// <summary>
    /// 레이어 선택
    /// </summary>
    public void SelectLayer(Guid layerId)
    {
        SelectedLayerId = layerId;
    }

    /// <summary>
    /// 레이어 선택 해제
    /// </summary>
    public void ClearSelection()
    {
        SelectedLayerId = null;
        IsDragging = false;
        IsResizing = false;
        IsRotating = false;
        CurrentResizeDirection = ResizeDirection.None;
    }

    /// <summary>
    /// 드래그 시작
    /// </summary>
    public void StartDrag(Point startPoint)
    {
        if (!SelectedLayerId.HasValue) return;

        IsDragging = true;
        _manipulationStartPoint = startPoint;
        _originalBounds = _canvas.GetLayerBounds(SelectedLayerId.Value) ?? new Rect();
    }

    /// <summary>
    /// 드래그 업데이트
    /// </summary>
    public void UpdateDrag(Point currentPoint)
    {
        if (!IsDragging || !SelectedLayerId.HasValue) return;

        var deltaX = currentPoint.X - _manipulationStartPoint.X;
        var deltaY = currentPoint.Y - _manipulationStartPoint.Y;

        var newX = _originalBounds.X + deltaX;
        var newY = _originalBounds.Y + deltaY;

        _canvas.UpdateLayerPosition(SelectedLayerId.Value, newX, newY);
    }

    /// <summary>
    /// 드래그 종료
    /// </summary>
    public void EndDrag()
    {
        if (!IsDragging || !SelectedLayerId.HasValue)
        {
            IsDragging = false;
            return;
        }

        var newBounds = _canvas.GetLayerBounds(SelectedLayerId.Value) ?? new Rect();
        if (Math.Abs(_originalBounds.X - newBounds.X) > 0.01 ||
            Math.Abs(_originalBounds.Y - newBounds.Y) > 0.01)
        {
            _undoService.Push(new MoveAction(_canvas, SelectedLayerId.Value, _originalBounds.X, _originalBounds.Y));
        }

        IsDragging = false;
    }

    /// <summary>
    /// 리사이즈 시작
    /// </summary>
    public void StartResize(Point startPoint, ResizeDirection direction)
    {
        if (!SelectedLayerId.HasValue || direction == ResizeDirection.None) return;

        IsResizing = true;
        CurrentResizeDirection = direction;
        _manipulationStartPoint = startPoint;
        _originalBounds = _canvas.GetLayerBounds(SelectedLayerId.Value) ?? new Rect();
    }

    /// <summary>
    /// 리사이즈 업데이트
    /// </summary>
    public void UpdateResize(Point currentPoint)
    {
        if (!IsResizing || !SelectedLayerId.HasValue) return;

        var deltaX = currentPoint.X - _manipulationStartPoint.X;
        var deltaY = currentPoint.Y - _manipulationStartPoint.Y;

        var newBounds = CalculateResizedBounds(deltaX, deltaY);
        _canvas.UpdateLayerTransform(
            SelectedLayerId.Value,
            newBounds.X, newBounds.Y,
            newBounds.Width, newBounds.Height);
    }

    /// <summary>
    /// 리사이즈 종료
    /// </summary>
    public void EndResize()
    {
        if (!IsResizing || !SelectedLayerId.HasValue)
        {
            IsResizing = false;
            CurrentResizeDirection = ResizeDirection.None;
            return;
        }

        var newBounds = _canvas.GetLayerBounds(SelectedLayerId.Value) ?? new Rect();
        if (Math.Abs(_originalBounds.Width - newBounds.Width) > 0.01 ||
            Math.Abs(_originalBounds.Height - newBounds.Height) > 0.01)
        {
            _undoService.Push(new ResizeAction(_canvas, SelectedLayerId.Value, _originalBounds));
        }

        IsResizing = false;
        CurrentResizeDirection = ResizeDirection.None;
    }

    /// <summary>
    /// 회전 시작
    /// </summary>
    public void StartRotation(Point startPoint)
    {
        if (!SelectedLayerId.HasValue) return;

        IsRotating = true;
        var bounds = _canvas.GetLayerBounds(SelectedLayerId.Value) ?? new Rect();
        _rotationCenter = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        _rotationStartAngle = GetAngleFromCenter(_rotationCenter, startPoint);
        _originalRotation = _canvas.GetLayerRotation(SelectedLayerId.Value);
    }

    /// <summary>
    /// 회전 업데이트
    /// </summary>
    public void UpdateRotation(Point currentPoint)
    {
        if (!IsRotating || !SelectedLayerId.HasValue) return;

        var currentAngle = GetAngleFromCenter(_rotationCenter, currentPoint);
        var deltaAngle = currentAngle - _rotationStartAngle;
        var newRotation = _originalRotation + deltaAngle;

        _canvas.UpdateLayerRotation(SelectedLayerId.Value, newRotation);
    }

    /// <summary>
    /// 회전 종료
    /// </summary>
    public void EndRotation()
    {
        if (!IsRotating || !SelectedLayerId.HasValue)
        {
            IsRotating = false;
            return;
        }

        var newRotation = _canvas.GetLayerRotation(SelectedLayerId.Value);
        if (Math.Abs(_originalRotation - newRotation) > 0.01)
        {
            _undoService.Push(new RotateAction(_canvas, SelectedLayerId.Value, _originalRotation));
        }

        IsRotating = false;
    }

    /// <summary>
    /// 모든 조작 종료
    /// </summary>
    public void EndAllManipulations()
    {
        if (IsDragging) EndDrag();
        if (IsResizing) EndResize();
        if (IsRotating) EndRotation();
    }

    /// <summary>
    /// 선택된 레이어의 현재 경계 반환
    /// </summary>
    public Rect? GetSelectedLayerBounds()
    {
        if (!SelectedLayerId.HasValue) return null;
        return _canvas.GetLayerBounds(SelectedLayerId.Value);
    }

    /// <summary>
    /// 선택된 레이어의 현재 회전값 반환
    /// </summary>
    public double GetSelectedLayerRotation()
    {
        if (!SelectedLayerId.HasValue) return 0;
        return _canvas.GetLayerRotation(SelectedLayerId.Value);
    }

    /// <summary>
    /// 리사이즈된 경계 계산
    /// </summary>
    private Rect CalculateResizedBounds(double deltaX, double deltaY)
    {
        var x = _originalBounds.X;
        var y = _originalBounds.Y;
        var width = _originalBounds.Width;
        var height = _originalBounds.Height;

        var aspectRatio = _originalBounds.Width / _originalBounds.Height;
        var isDiagonal = CurrentResizeDirection is ResizeDirection.TopLeft or ResizeDirection.TopRight
                         or ResizeDirection.BottomLeft or ResizeDirection.BottomRight;

        switch (CurrentResizeDirection)
        {
            case ResizeDirection.TopLeft:
                {
                    var newWidth = width - deltaX;
                    var newHeight = newWidth / aspectRatio;
                    x = _originalBounds.Right - newWidth;
                    y = _originalBounds.Bottom - newHeight;
                    width = newWidth;
                    height = newHeight;
                }
                break;
            case ResizeDirection.Top:
                y += deltaY;
                height -= deltaY;
                break;
            case ResizeDirection.TopRight:
                {
                    var newWidth = width + deltaX;
                    var newHeight = newWidth / aspectRatio;
                    y = _originalBounds.Bottom - newHeight;
                    width = newWidth;
                    height = newHeight;
                }
                break;
            case ResizeDirection.Right:
                width += deltaX;
                break;
            case ResizeDirection.BottomRight:
                {
                    var newWidth = width + deltaX;
                    var newHeight = newWidth / aspectRatio;
                    width = newWidth;
                    height = newHeight;
                }
                break;
            case ResizeDirection.Bottom:
                height += deltaY;
                break;
            case ResizeDirection.BottomLeft:
                {
                    var newWidth = width - deltaX;
                    var newHeight = newWidth / aspectRatio;
                    x = _originalBounds.Right - newWidth;
                    width = newWidth;
                    height = newHeight;
                }
                break;
            case ResizeDirection.Left:
                x += deltaX;
                width -= deltaX;
                break;
        }

        // 최소 크기 적용
        if (width < MinSize)
        {
            if (isDiagonal)
            {
                width = MinSize;
                height = MinSize / aspectRatio;
            }
            else
            {
                width = MinSize;
            }

            if (CurrentResizeDirection is ResizeDirection.TopLeft or ResizeDirection.Left or ResizeDirection.BottomLeft)
            {
                x = _originalBounds.Right - width;
            }
        }

        if (height < MinSize)
        {
            if (isDiagonal)
            {
                height = MinSize;
                width = MinSize * aspectRatio;
            }
            else
            {
                height = MinSize;
            }

            if (CurrentResizeDirection is ResizeDirection.TopLeft or ResizeDirection.Top or ResizeDirection.TopRight)
            {
                y = _originalBounds.Bottom - height;
            }
        }

        return new Rect(x, y, width, height);
    }

    /// <summary>
    /// 중심점으로부터 각도 계산
    /// </summary>
    private static double GetAngleFromCenter(Point center, Point point)
    {
        return Math.Atan2(point.Y - center.Y, point.X - center.X) * 180 / Math.PI;
    }
}

/// <summary>
/// 리사이즈 방향
/// </summary>
public enum ResizeDirection
{
    None,
    TopLeft, Top, TopRight,
    Left, Right,
    BottomLeft, Bottom, BottomRight
}

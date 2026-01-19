using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using Cursor = System.Windows.Input.Cursor;
using Cursors = System.Windows.Input.Cursors;
using Brushes = System.Windows.Media.Brushes;

namespace Wonderland.WpfApp.Services.Editing;

/// <summary>
/// 선택 표시 서비스
/// 선택 사각형, 리사이즈 핸들, 회전 핸들 UI 요소를 관리
/// </summary>
public sealed class SelectionIndicatorService
{
    private readonly Canvas _overlay;

    private Rectangle? _selectionRect;
    private Ellipse? _rotationHandle;
    private readonly List<Rectangle> _resizeHandles = [];

    // 상수
    private const double HandleSize = 8;
    private const double RotationHandleSize = 12;
    private const double RotationHandleOffset = 30;

    public SelectionIndicatorService(Canvas overlay)
    {
        _overlay = overlay;
    }

    /// <summary>
    /// 선택 표시 요소 생성
    /// </summary>
    public void CreateIndicators()
    {
        ClearIndicators();

        // 선택 사각형 생성
        _selectionRect = new Rectangle
        {
            Stroke = Brushes.Cyan,
            StrokeThickness = 2,
            StrokeDashArray = [4, 2],
            Fill = Brushes.Transparent
        };
        _overlay.Children.Add(_selectionRect);

        // 리사이즈 핸들 생성 (8개)
        for (var i = 0; i < 8; i++)
        {
            var handle = new Rectangle
            {
                Width = HandleSize,
                Height = HandleSize,
                Fill = Brushes.White,
                Stroke = Brushes.Cyan,
                StrokeThickness = 1
            };
            _resizeHandles.Add(handle);
            _overlay.Children.Add(handle);
        }

        // 회전 핸들 생성
        _rotationHandle = new Ellipse
        {
            Width = RotationHandleSize,
            Height = RotationHandleSize,
            Fill = Brushes.LimeGreen,
            Stroke = Brushes.White,
            StrokeThickness = 1,
            Cursor = Cursors.Hand
        };
        _overlay.Children.Add(_rotationHandle);
    }

    /// <summary>
    /// 선택 표시 요소 제거
    /// </summary>
    public void ClearIndicators()
    {
        if (_selectionRect is not null)
        {
            _overlay.Children.Remove(_selectionRect);
            _selectionRect = null;
        }

        if (_rotationHandle is not null)
        {
            _overlay.Children.Remove(_rotationHandle);
            _rotationHandle = null;
        }

        foreach (var handle in _resizeHandles)
        {
            _overlay.Children.Remove(handle);
        }
        _resizeHandles.Clear();
    }

    /// <summary>
    /// 선택 표시 요소 위치 업데이트
    /// </summary>
    public void UpdateIndicators(Rect bounds, double rotation)
    {
        if (_selectionRect is null) return;

        // 선택 사각형 위치 및 회전
        Canvas.SetLeft(_selectionRect, bounds.X);
        Canvas.SetTop(_selectionRect, bounds.Y);
        _selectionRect.Width = bounds.Width;
        _selectionRect.Height = bounds.Height;
        _selectionRect.RenderTransform = new RotateTransform(rotation, bounds.Width / 2, bounds.Height / 2);

        // 핸들 위치 (회전 전 로컬 좌표)
        var halfHandle = HandleSize / 2;
        var localPositions = new Point[]
        {
            new(-halfHandle, -halfHandle),                                  // TopLeft
            new(bounds.Width / 2 - halfHandle, -halfHandle),                // Top
            new(bounds.Width - halfHandle, -halfHandle),                    // TopRight
            new(bounds.Width - halfHandle, bounds.Height / 2 - halfHandle), // Right
            new(bounds.Width - halfHandle, bounds.Height - halfHandle),     // BottomRight
            new(bounds.Width / 2 - halfHandle, bounds.Height - halfHandle), // Bottom
            new(-halfHandle, bounds.Height - halfHandle),                   // BottomLeft
            new(-halfHandle, bounds.Height / 2 - halfHandle)                // Left
        };

        // 회전 변환 행렬
        var rotateMatrix = new Matrix();
        rotateMatrix.RotateAt(rotation, bounds.Width / 2, bounds.Height / 2);

        for (var i = 0; i < _resizeHandles.Count && i < localPositions.Length; i++)
        {
            var rotatedPoint = rotateMatrix.Transform(localPositions[i]);
            Canvas.SetLeft(_resizeHandles[i], bounds.X + rotatedPoint.X);
            Canvas.SetTop(_resizeHandles[i], bounds.Y + rotatedPoint.Y);
        }

        // 회전 핸들 위치
        if (_rotationHandle is not null)
        {
            var localRotationPos = new Point(
                bounds.Width / 2 - RotationHandleSize / 2,
                -RotationHandleOffset - RotationHandleSize / 2);
            var rotatedRotationPos = rotateMatrix.Transform(localRotationPos);
            Canvas.SetLeft(_rotationHandle, bounds.X + rotatedRotationPos.X);
            Canvas.SetTop(_rotationHandle, bounds.Y + rotatedRotationPos.Y);
        }
    }

    /// <summary>
    /// 회전 핸들 위에 포인트가 있는지 확인
    /// </summary>
    public bool IsPointOnRotationHandle(Point point)
    {
        if (_rotationHandle is null) return false;

        var handleLeft = Canvas.GetLeft(_rotationHandle);
        var handleTop = Canvas.GetTop(_rotationHandle);

        if (double.IsNaN(handleLeft) || double.IsNaN(handleTop)) return false;

        var handleRect = new Rect(handleLeft, handleTop, RotationHandleSize, RotationHandleSize);
        return handleRect.Contains(point);
    }

    /// <summary>
    /// 마우스 위치로부터 리사이즈 방향 결정
    /// </summary>
    public ResizeDirection GetResizeDirection(Point pos)
    {
        if (_resizeHandles.Count < 8) return ResizeDirection.None;

        var directions = new[]
        {
            ResizeDirection.TopLeft, ResizeDirection.Top, ResizeDirection.TopRight,
            ResizeDirection.Right, ResizeDirection.BottomRight,
            ResizeDirection.Bottom, ResizeDirection.BottomLeft, ResizeDirection.Left
        };

        for (var i = 0; i < _resizeHandles.Count; i++)
        {
            var handle = _resizeHandles[i];
            var handleRect = new Rect(
                Canvas.GetLeft(handle),
                Canvas.GetTop(handle),
                HandleSize,
                HandleSize);

            if (handleRect.Contains(pos))
            {
                return directions[i];
            }
        }

        return ResizeDirection.None;
    }

    /// <summary>
    /// 리사이즈 방향에 따른 커서 반환
    /// </summary>
    public static Cursor GetResizeCursor(ResizeDirection direction)
    {
        return direction switch
        {
            ResizeDirection.TopLeft or ResizeDirection.BottomRight => Cursors.SizeNWSE,
            ResizeDirection.TopRight or ResizeDirection.BottomLeft => Cursors.SizeNESW,
            ResizeDirection.Top or ResizeDirection.Bottom => Cursors.SizeNS,
            ResizeDirection.Left or ResizeDirection.Right => Cursors.SizeWE,
            _ => Cursors.Arrow
        };
    }
}

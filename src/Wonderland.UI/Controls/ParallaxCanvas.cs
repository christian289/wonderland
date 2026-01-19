using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wonderland.Domain.Entities;

namespace Wonderland.UI.Controls;

/// <summary>
/// Parallax 효과 렌더링 캔버스 (DrawingVisual 기반 고성능)
/// Parallax effect rendering canvas (High-performance based on DrawingVisual)
/// </summary>
public sealed class ParallaxCanvas : FrameworkElement
{
    private readonly ContainerVisual _rootVisual = new();
    private readonly Dictionary<Guid, DrawingVisual> _layerVisuals = [];
    private readonly Dictionary<Guid, BitmapSource> _layerImages = [];
    private readonly Dictionary<Guid, LayerRenderInfo> _layerInfos = [];

    /// <summary>
    /// 레이어 렌더링 정보
    /// Layer rendering information
    /// </summary>
    private sealed record LayerRenderInfo(
        double X,
        double Y,
        double Width,
        double Height,
        int ZIndex,
        double Rotation = 0);

    public ParallaxCanvas()
    {
        AddVisualChild(_rootVisual);
    }

    protected override int VisualChildrenCount => 1;
    protected override Visual GetVisualChild(int index) => _rootVisual;

    /// <summary>
    /// 레이어 추가
    /// Add layer
    /// </summary>
    public void AddLayer(Layer layer)
    {
        if (string.IsNullOrEmpty(layer.ImagePath) || !System.IO.File.Exists(layer.ImagePath))
            return;

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(layer.ImagePath, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();

            _layerImages[layer.Id] = image;

            var visual = new DrawingVisual();
            _layerVisuals[layer.Id] = visual;

            // 이미지 크기 계산 (창 크기에 맞게 스케일링)
            // Calculate image size (scale to fit window)
            var (scaledWidth, scaledHeight) = CalculateScaledSize(
                image.PixelWidth,
                image.PixelHeight,
                layer.Transform.Width > 0 ? layer.Transform.Width : ActualWidth,
                layer.Transform.Height > 0 ? layer.Transform.Height : ActualHeight);

            _layerInfos[layer.Id] = new LayerRenderInfo(
                layer.Transform.X,
                layer.Transform.Y,
                scaledWidth,
                scaledHeight,
                layer.ZIndex,
                layer.Transform.Rotation);

            InsertVisualByZIndex(visual, layer.ZIndex);
            RenderLayer(layer.Id, 0, 0);
        }
        catch
        {
            // 이미지 로드 실패 시 무시
            // Ignore if image load fails
        }
    }

    /// <summary>
    /// 종횡비 유지하며 크기 계산 (Contain 모드 - 이미지 전체가 보이도록)
    /// Calculate size maintaining aspect ratio (Contain mode - entire image visible)
    /// </summary>
    private static (double Width, double Height) CalculateScaledSize(
        double imageWidth,
        double imageHeight,
        double maxWidth,
        double maxHeight)
    {
        if (maxWidth <= 0 || maxHeight <= 0)
        {
            return (imageWidth, imageHeight);
        }

        var ratioX = maxWidth / imageWidth;
        var ratioY = maxHeight / imageHeight;
        var ratio = Math.Min(ratioX, ratioY);

        // 이미지가 창보다 작으면 원본 크기 유지
        // Keep original size if image is smaller than window
        if (ratio >= 1)
        {
            return (imageWidth, imageHeight);
        }

        return (imageWidth * ratio, imageHeight * ratio);
    }

    /// <summary>
    /// 종횡비 유지하며 영역을 채우도록 크기 계산 (Cover 모드 - 영역 전체를 덮음)
    /// Calculate size to cover area maintaining aspect ratio (Cover mode - fills entire area)
    /// </summary>
    private static (double Width, double Height, double X, double Y) CalculateCoverSize(
        double imageWidth,
        double imageHeight,
        double targetWidth,
        double targetHeight)
    {
        if (targetWidth <= 0 || targetHeight <= 0)
        {
            return (imageWidth, imageHeight, 0, 0);
        }

        var ratioX = targetWidth / imageWidth;
        var ratioY = targetHeight / imageHeight;
        var ratio = Math.Max(ratioX, ratioY);

        var scaledWidth = imageWidth * ratio;
        var scaledHeight = imageHeight * ratio;

        // 중앙 정렬
        // Center alignment
        var x = (targetWidth - scaledWidth) / 2;
        var y = (targetHeight - scaledHeight) / 2;

        return (scaledWidth, scaledHeight, x, y);
    }

    /// <summary>
    /// 배경 레이어를 창 크기에 맞게 스케일
    /// Scale background layer to fit window size
    /// </summary>
    public void ScaleBackgroundToFit(Guid layerId, double windowWidth, double windowHeight)
    {
        if (!_layerImages.TryGetValue(layerId, out var image) ||
            !_layerInfos.TryGetValue(layerId, out var info))
        {
            return;
        }

        var (width, height, x, y) = CalculateCoverSize(
            image.PixelWidth,
            image.PixelHeight,
            windowWidth,
            windowHeight);

        _layerInfos[layerId] = info with { X = x, Y = y, Width = width, Height = height };
        RenderLayer(layerId, 0, 0);
    }

    /// <summary>
    /// 레이어가 배경인지 확인 (Z-Index 0)
    /// Check if layer is background (Z-Index 0)
    /// </summary>
    public bool IsBackgroundLayer(Guid layerId)
    {
        return _layerInfos.TryGetValue(layerId, out var info) && info.ZIndex == 0;
    }

    /// <summary>
    /// 레이어 제거
    /// Remove layer
    /// </summary>
    public void RemoveLayer(Guid layerId)
    {
        if (_layerVisuals.TryGetValue(layerId, out var visual))
        {
            _rootVisual.Children.Remove(visual);
            _layerVisuals.Remove(layerId);
        }

        _layerImages.Remove(layerId);
        _layerInfos.Remove(layerId);
    }

    /// <summary>
    /// 모든 레이어 제거
    /// Clear all layers
    /// </summary>
    public void ClearLayers()
    {
        _rootVisual.Children.Clear();
        _layerVisuals.Clear();
        _layerImages.Clear();
        _layerInfos.Clear();
    }

    /// <summary>
    /// 레이어 오프셋 업데이트 (Parallax 효과)
    /// Update layer offset (Parallax effect)
    /// </summary>
    public void UpdateLayerOffset(Guid layerId, double offsetX, double offsetY)
    {
        if (!_layerVisuals.TryGetValue(layerId, out var visual))
            return;

        visual.Offset = new Vector(offsetX, offsetY);
    }

    /// <summary>
    /// 모든 레이어 오프셋 일괄 업데이트
    /// Batch update all layer offsets
    /// </summary>
    public void UpdateAllOffsets(IEnumerable<(Guid LayerId, double OffsetX, double OffsetY)> offsets)
    {
        foreach (var (layerId, offsetX, offsetY) in offsets)
        {
            UpdateLayerOffset(layerId, offsetX, offsetY);
        }
    }

    /// <summary>
    /// 레이어 Z-Index 업데이트
    /// Update layer Z-Index
    /// </summary>
    public void UpdateLayerZIndex(Guid layerId, int newZIndex)
    {
        if (!_layerVisuals.TryGetValue(layerId, out var visual) ||
            !_layerInfos.TryGetValue(layerId, out var info))
        {
            return;
        }

        // 기존 Visual 제거
        // Remove existing visual
        _rootVisual.Children.Remove(visual);

        // 새 ZIndex로 정보 업데이트
        // Update info with new ZIndex
        _layerInfos[layerId] = info with { ZIndex = newZIndex };

        // 새 위치에 삽입
        // Insert at new position
        InsertVisualByZIndex(visual, newZIndex);
    }

    /// <summary>
    /// 레이어 렌더링
    /// Render layer
    /// </summary>
    private void RenderLayer(Guid layerId, double offsetX, double offsetY)
    {
        if (!_layerVisuals.TryGetValue(layerId, out var visual) ||
            !_layerImages.TryGetValue(layerId, out var image) ||
            !_layerInfos.TryGetValue(layerId, out var info))
            return;

        using var dc = visual.RenderOpen();

        // 회전 변환 적용
        // Apply rotation transform
        if (info.Rotation != 0)
        {
            var centerX = info.X + info.Width / 2;
            var centerY = info.Y + info.Height / 2;
            dc.PushTransform(new RotateTransform(info.Rotation, centerX, centerY));
        }

        dc.DrawImage(image, new Rect(info.X, info.Y, info.Width, info.Height));

        if (info.Rotation != 0)
        {
            dc.Pop();
        }
    }

    /// <summary>
    /// ZIndex 순서로 Visual 삽입
    /// Insert Visual in ZIndex order
    /// </summary>
    private void InsertVisualByZIndex(DrawingVisual visual, int zIndex)
    {
        var insertIndex = 0;

        foreach (var existing in _layerVisuals)
        {
            if (_layerInfos.TryGetValue(existing.Key, out var info) && info.ZIndex <= zIndex)
            {
                insertIndex++;
            }
        }

        if (insertIndex >= _rootVisual.Children.Count)
        {
            _rootVisual.Children.Add(visual);
        }
        else
        {
            _rootVisual.Children.Insert(insertIndex, visual);
        }
    }

    /// <summary>
    /// 레이어 다시 그리기 (이미지 변경 시)
    /// Redraw layer (when image changes)
    /// </summary>
    public void RedrawLayer(Guid layerId)
    {
        if (_layerInfos.TryGetValue(layerId, out var info))
        {
            RenderLayer(layerId, 0, 0);
        }
    }

    /// <summary>
    /// 특정 좌표에서 레이어 히트 테스트
    /// Hit test layer at specific point
    /// </summary>
    public Guid? HitTestLayer(Point point)
    {
        // Z-Index 역순으로 검사 (위에 있는 레이어 우선)
        // Check in reverse Z-Index order (top layer first)
        var sortedLayers = _layerInfos
            .OrderByDescending(kvp => kvp.Value.ZIndex)
            .ToList();

        foreach (var (layerId, info) in sortedLayers)
        {
            var bounds = new Rect(info.X, info.Y, info.Width, info.Height);
            if (bounds.Contains(point))
            {
                return layerId;
            }
        }

        return null;
    }

    /// <summary>
    /// 레이어 경계 영역 반환
    /// Get layer bounds
    /// </summary>
    public Rect? GetLayerBounds(Guid layerId)
    {
        if (!_layerInfos.TryGetValue(layerId, out var info))
        {
            return null;
        }

        return new Rect(info.X, info.Y, info.Width, info.Height);
    }

    /// <summary>
    /// 레이어 위치 업데이트
    /// Update layer position
    /// </summary>
    public void UpdateLayerPosition(Guid layerId, double x, double y)
    {
        if (!_layerInfos.TryGetValue(layerId, out var info))
        {
            return;
        }

        _layerInfos[layerId] = info with { X = x, Y = y };
        RenderLayer(layerId, 0, 0);
    }

    /// <summary>
    /// 레이어 크기 업데이트
    /// Update layer size
    /// </summary>
    public void UpdateLayerSize(Guid layerId, double width, double height)
    {
        if (!_layerInfos.TryGetValue(layerId, out var info))
        {
            return;
        }

        _layerInfos[layerId] = info with { Width = width, Height = height };
        RenderLayer(layerId, 0, 0);
    }

    /// <summary>
    /// 레이어 위치 및 크기 업데이트
    /// Update layer position and size
    /// </summary>
    public void UpdateLayerTransform(Guid layerId, double x, double y, double width, double height)
    {
        if (!_layerInfos.TryGetValue(layerId, out var info))
        {
            return;
        }

        _layerInfos[layerId] = info with { X = x, Y = y, Width = width, Height = height };
        RenderLayer(layerId, 0, 0);
    }

    /// <summary>
    /// 레이어 회전 업데이트
    /// Update layer rotation
    /// </summary>
    public void UpdateLayerRotation(Guid layerId, double rotation)
    {
        if (!_layerInfos.TryGetValue(layerId, out var info))
        {
            return;
        }

        _layerInfos[layerId] = info with { Rotation = rotation };
        RenderLayer(layerId, 0, 0);
    }

    /// <summary>
    /// 레이어 회전 값 가져오기
    /// Get layer rotation
    /// </summary>
    public double GetLayerRotation(Guid layerId)
    {
        return _layerInfos.TryGetValue(layerId, out var info) ? info.Rotation : 0;
    }

    /// <summary>
    /// 모든 레이어 ID 반환
    /// Get all layer IDs
    /// </summary>
    public IEnumerable<Guid> GetAllLayerIds() => _layerInfos.Keys;
}

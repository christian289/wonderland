using CommunityToolkit.Mvvm.ComponentModel;
using Wonderland.Domain.Entities;
using Wonderland.Domain.ValueObjects;

namespace Wonderland.ViewModels;

/// <summary>
/// 레이어 뷰모델
/// </summary>
public sealed partial class LayerViewModel : ObservableObject
{
    private readonly Layer _layer;

    public LayerViewModel(Layer layer)
    {
        _layer = layer;
    }

    /// <summary>
    /// 레이어 ID
    /// </summary>
    public Guid Id => _layer.Id;

    /// <summary>
    /// 레이어 이름
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// 이미지 경로
    /// </summary>
    [ObservableProperty]
    private string _imagePath = string.Empty;

    /// <summary>
    /// Z 순서
    /// </summary>
    [ObservableProperty]
    private int _zIndex;

    /// <summary>
    /// 표시 여부
    /// </summary>
    [ObservableProperty]
    private bool _isVisible = true;

    /// <summary>
    /// 깊이 배율
    /// </summary>
    [ObservableProperty]
    private double _depthFactor = 1.0;

    /// <summary>
    /// 최대 X 오프셋
    /// </summary>
    [ObservableProperty]
    private double _maxOffsetX = 50.0;

    /// <summary>
    /// 최대 Y 오프셋
    /// </summary>
    [ObservableProperty]
    private double _maxOffsetY = 30.0;

    /// <summary>
    /// X축 반전
    /// </summary>
    [ObservableProperty]
    private bool _invertX;

    /// <summary>
    /// Y축 반전
    /// </summary>
    [ObservableProperty]
    private bool _invertY;

    /// <summary>
    /// 현재 X 오프셋 (런타임)
    /// </summary>
    [ObservableProperty]
    private double _currentOffsetX;

    /// <summary>
    /// 현재 Y 오프셋 (런타임)
    /// </summary>
    [ObservableProperty]
    private double _currentOffsetY;

    /// <summary>
    /// Layer 엔티티에서 ViewModel 로드
    /// </summary>
    public void LoadFromEntity()
    {
        Name = _layer.Name;
        ImagePath = _layer.ImagePath;
        ZIndex = _layer.ZIndex;
        IsVisible = _layer.IsVisible;
        DepthFactor = _layer.Parallax.DepthFactor;
        MaxOffsetX = _layer.Parallax.MaxOffsetX;
        MaxOffsetY = _layer.Parallax.MaxOffsetY;
        InvertX = _layer.Parallax.InvertX;
        InvertY = _layer.Parallax.InvertY;
    }

    /// <summary>
    /// ViewModel에서 Layer 엔티티로 저장
    /// </summary>
    public void SaveToEntity()
    {
        _layer.Name = Name;
        _layer.ImagePath = ImagePath;
        _layer.ZIndex = ZIndex;
        _layer.IsVisible = IsVisible;
        _layer.Parallax = new ParallaxSettings
        {
            DepthFactor = DepthFactor,
            MaxOffsetX = MaxOffsetX,
            MaxOffsetY = MaxOffsetY,
            InvertX = InvertX,
            InvertY = InvertY
        };
    }

    /// <summary>
    /// 내부 Layer 엔티티 반환
    /// </summary>
    public Layer GetEntity() => _layer;
}

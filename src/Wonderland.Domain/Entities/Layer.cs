using Wonderland.Domain.ValueObjects;

namespace Wonderland.Domain.Entities;

/// <summary>
/// 이미지 레이어 엔티티
/// Image layer entity
/// </summary>
public sealed class Layer
{
    /// <summary>
    /// 레이어 고유 ID
    /// Layer unique ID
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// 레이어 이름
    /// Layer name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 이미지 파일 경로
    /// Image file path
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Z 순서 (0 = 배경, 1-10 = 전경)
    /// Z-order (0 = background, 1-10 = foreground)
    /// </summary>
    public int ZIndex { get; set; }

    /// <summary>
    /// 변환 정보 (위치, 크기, 회전)
    /// Transform information (position, size, rotation)
    /// </summary>
    public LayerTransform Transform { get; set; } = new();

    /// <summary>
    /// Parallax 설정
    /// Parallax settings
    /// </summary>
    public ParallaxSettings Parallax { get; set; } = new();

    /// <summary>
    /// 표시 여부
    /// Visibility
    /// </summary>
    public bool IsVisible { get; set; } = true;
}

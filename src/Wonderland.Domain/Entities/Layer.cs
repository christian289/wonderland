using Wonderland.Domain.ValueObjects;

namespace Wonderland.Domain.Entities;

/// <summary>
/// 이미지 레이어 엔티티
/// </summary>
public sealed class Layer
{
    /// <summary>
    /// 레이어 고유 ID
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// 레이어 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 이미지 파일 경로
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Z 순서 (0 = 배경, 1-10 = 전경)
    /// </summary>
    public int ZIndex { get; set; }

    /// <summary>
    /// 변환 정보 (위치, 크기, 회전)
    /// </summary>
    public LayerTransform Transform { get; set; } = new();

    /// <summary>
    /// Parallax 설정
    /// </summary>
    public ParallaxSettings Parallax { get; set; } = new();

    /// <summary>
    /// 표시 여부
    /// </summary>
    public bool IsVisible { get; set; } = true;
}

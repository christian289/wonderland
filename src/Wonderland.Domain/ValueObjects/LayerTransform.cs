namespace Wonderland.Domain.ValueObjects;

/// <summary>
/// 레이어 변환 정보 (위치, 크기, 회전)
/// Layer transform information (position, size, rotation)
/// </summary>
public sealed record LayerTransform
{
    /// <summary>
    /// X 좌표
    /// X coordinate
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Y 좌표
    /// Y coordinate
    /// </summary>
    public double Y { get; init; }

    /// <summary>
    /// 너비
    /// Width
    /// </summary>
    public double Width { get; init; }

    /// <summary>
    /// 높이
    /// Height
    /// </summary>
    public double Height { get; init; }

    /// <summary>
    /// 회전 각도 (도)
    /// Rotation angle (degrees)
    /// </summary>
    public double Rotation { get; init; }

    /// <summary>
    /// 크기 배율
    /// Scale factor
    /// </summary>
    public double Scale { get; init; } = 1.0;
}

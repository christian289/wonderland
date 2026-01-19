namespace Wonderland.Domain.ValueObjects;

/// <summary>
/// 레이어 변환 정보 (위치, 크기, 회전)
/// </summary>
public sealed record LayerTransform
{
    /// <summary>
    /// X 좌표
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Y 좌표
    /// </summary>
    public double Y { get; init; }

    /// <summary>
    /// 너비
    /// </summary>
    public double Width { get; init; }

    /// <summary>
    /// 높이
    /// </summary>
    public double Height { get; init; }

    /// <summary>
    /// 회전 각도 (도)
    /// </summary>
    public double Rotation { get; init; }

    /// <summary>
    /// 크기 배율
    /// </summary>
    public double Scale { get; init; } = 1.0;
}

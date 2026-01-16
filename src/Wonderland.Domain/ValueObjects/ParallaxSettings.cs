namespace Wonderland.Domain.ValueObjects;

/// <summary>
/// Parallax 효과 설정
/// Parallax effect settings
/// </summary>
public sealed record ParallaxSettings
{
    /// <summary>
    /// 깊이 배율 (0.0 ~ 2.0)
    /// 높을수록 마우스 이동에 더 많이 반응
    /// Depth factor (0.0 ~ 2.0)
    /// Higher values mean more movement in response to mouse
    /// </summary>
    public double DepthFactor { get; init; } = 1.0;

    /// <summary>
    /// 최대 X축 이동량 (픽셀)
    /// Maximum X-axis offset (pixels)
    /// </summary>
    public double MaxOffsetX { get; init; } = 50.0;

    /// <summary>
    /// 최대 Y축 이동량 (픽셀)
    /// Maximum Y-axis offset (pixels)
    /// </summary>
    public double MaxOffsetY { get; init; } = 30.0;

    /// <summary>
    /// X축 반전 여부
    /// Whether to invert X-axis movement
    /// </summary>
    public bool InvertX { get; init; }

    /// <summary>
    /// Y축 반전 여부
    /// Whether to invert Y-axis movement
    /// </summary>
    public bool InvertY { get; init; }
}

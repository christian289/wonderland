using Wonderland.Domain.ValueObjects;

namespace Wonderland.Application.Services;

/// <summary>
/// Parallax 오프셋 계산 서비스
/// </summary>
public sealed class ParallaxCalculator
{
    /// <summary>
    /// 마우스 위치 기반 레이어 오프셋 계산
    /// </summary>
    /// <param name="mouseNormalizedX">정규화된 마우스 X 좌표 (-1.0 ~ 1.0)</param>
    /// <param name="mouseNormalizedY">정규화된 마우스 Y 좌표 (-1.0 ~ 1.0)</param>
    /// <param name="settings">Parallax 설정</param>
    /// <returns>오프셋 (X, Y)</returns>
    public (double OffsetX, double OffsetY) CalculateOffset(
        double mouseNormalizedX,
        double mouseNormalizedY,
        ParallaxSettings settings)
    {
        // 깊이 기반 이동량 계산
        var offsetX = mouseNormalizedX * settings.MaxOffsetX * settings.DepthFactor;
        var offsetY = mouseNormalizedY * settings.MaxOffsetY * settings.DepthFactor;

        // 반전 적용
        if (settings.InvertX) offsetX = -offsetX;
        if (settings.InvertY) offsetY = -offsetY;

        return (offsetX, offsetY);
    }

    /// <summary>
    /// ZIndex 기반 기본 DepthFactor 추천값 반환
    /// </summary>
    /// <param name="zIndex">레이어 Z순서 (0-10)</param>
    /// <returns>추천 DepthFactor</returns>
    public static double GetRecommendedDepthFactor(int zIndex) => zIndex switch
    {
        0 => 0.1,           // 배경
        1 or 2 or 3 => 0.3, // 원경
        4 or 5 or 6 => 0.6, // 중경
        _ => 1.0 + (zIndex - 7) * 0.15  // 근경
    };
}

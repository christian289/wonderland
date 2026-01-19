using System.Windows;

namespace Wonderland.WpfServices;

/// <summary>
/// 마우스 추적 서비스
/// </summary>
public sealed class MouseTrackingService
{
    private System.Windows.Point _screenCenter;
    private System.Windows.Point _lastPosition;
    private System.Windows.Point _smoothedPosition;
    private readonly double _smoothingFactor;
    private double _halfScreenWidth;
    private double _halfScreenHeight;

    /// <summary>
    /// 스무딩 팩터 (0.0 ~ 1.0, 높을수록 즉각 반응)
    /// </summary>
    public MouseTrackingService(double smoothingFactor = 0.15)
    {
        _smoothingFactor = Math.Clamp(smoothingFactor, 0.01, 1.0);
    }

    /// <summary>
    /// 화면 크기로 초기화 (전역 마우스 훅은 화면 좌표를 반환하므로)
    /// </summary>
    public void Initialize(double windowWidth, double windowHeight)
    {
        // 가상 화면 크기 사용 (다중 모니터 지원)
        var virtualLeft = SystemParameters.VirtualScreenLeft;
        var virtualTop = SystemParameters.VirtualScreenTop;
        var virtualWidth = SystemParameters.VirtualScreenWidth;
        var virtualHeight = SystemParameters.VirtualScreenHeight;

        // 가상 화면의 중앙 계산
        var centerX = virtualLeft + virtualWidth / 2;
        var centerY = virtualTop + virtualHeight / 2;

        _halfScreenWidth = virtualWidth / 2;
        _halfScreenHeight = virtualHeight / 2;
        _screenCenter = new System.Windows.Point(centerX, centerY);
        _smoothedPosition = _screenCenter;
        _lastPosition = _screenCenter;
    }

    /// <summary>
    /// 창 크기 업데이트 (화면 크기 기반으로 다시 계산)
    /// </summary>
    public void UpdateWindowSize(double windowWidth, double windowHeight)
    {
        // 화면 크기는 변하지 않으므로 재계산 불필요
    }

    /// <summary>
    /// 마우스 위치를 정규화된 좌표로 변환 (-1 ~ 1)
    /// </summary>
    public (double NormalizedX, double NormalizedY) GetNormalizedPosition(System.Windows.Point mousePosition)
    {
        // 스무딩 적용
        _smoothedPosition = new System.Windows.Point(
            _smoothedPosition.X + (_smoothingFactor * (mousePosition.X - _smoothedPosition.X)),
            _smoothedPosition.Y + (_smoothingFactor * (mousePosition.Y - _smoothedPosition.Y))
        );

        _lastPosition = mousePosition;

        // 화면 중앙 기준 오프셋 계산
        var deltaX = _smoothedPosition.X - _screenCenter.X;
        var deltaY = _smoothedPosition.Y - _screenCenter.Y;

        // 정규화 (-1 ~ 1 범위)
        var normalizedX = _halfScreenWidth > 0
            ? Math.Clamp(deltaX / _halfScreenWidth, -1.0, 1.0)
            : 0;
        var normalizedY = _halfScreenHeight > 0
            ? Math.Clamp(deltaY / _halfScreenHeight, -1.0, 1.0)
            : 0;

        return (normalizedX, normalizedY);
    }

    /// <summary>
    /// 스무딩된 위치 초기화 (중앙으로)
    /// </summary>
    public void ResetToCenter()
    {
        _smoothedPosition = _screenCenter;
        _lastPosition = _screenCenter;
    }
}

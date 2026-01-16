namespace Wonderland.Domain.ValueObjects;

/// <summary>
/// 파티클 설정
/// Particle settings
/// </summary>
public sealed record ParticleSettings
{
    /// <summary>
    /// 최대 파티클 수
    /// Maximum number of particles
    /// </summary>
    public int MaxParticles { get; init; } = 100;

    /// <summary>
    /// 초당 생성 수
    /// Spawn rate per second
    /// </summary>
    public double SpawnRate { get; init; } = 10.0;

    /// <summary>
    /// 최소 크기 (픽셀)
    /// Minimum size (pixels)
    /// </summary>
    public double MinSize { get; init; } = 2.0;

    /// <summary>
    /// 최대 크기 (픽셀)
    /// Maximum size (pixels)
    /// </summary>
    public double MaxSize { get; init; } = 6.0;

    /// <summary>
    /// 최소 속도 (픽셀/초)
    /// Minimum speed (pixels/second)
    /// </summary>
    public double MinSpeed { get; init; } = 50.0;

    /// <summary>
    /// 최대 속도 (픽셀/초)
    /// Maximum speed (pixels/second)
    /// </summary>
    public double MaxSpeed { get; init; } = 150.0;

    /// <summary>
    /// 바람 강도 (X축 영향)
    /// Wind strength (X-axis influence)
    /// </summary>
    public double WindStrength { get; init; }

    /// <summary>
    /// 투명도 (0.0 ~ 1.0)
    /// Opacity (0.0 ~ 1.0)
    /// </summary>
    public double Opacity { get; init; } = 0.8;
}

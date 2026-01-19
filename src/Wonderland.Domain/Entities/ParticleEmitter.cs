using Wonderland.Domain.Enums;
using Wonderland.Domain.ValueObjects;

namespace Wonderland.Domain.Entities;

/// <summary>
/// 파티클 이미터 엔티티
/// </summary>
public sealed class ParticleEmitter
{
    /// <summary>
    /// 이미터 고유 ID
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// 파티클 타입
    /// </summary>
    public ParticleType Type { get; set; } = ParticleType.Snow;

    /// <summary>
    /// 파티클 설정
    /// </summary>
    public ParticleSettings Settings { get; set; } = new();

    /// <summary>
    /// 활성화 여부
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

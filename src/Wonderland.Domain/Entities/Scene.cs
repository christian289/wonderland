namespace Wonderland.Domain.Entities;

/// <summary>
/// 씬(프로젝트) 엔티티
/// </summary>
public sealed class Scene
{
    /// <summary>
    /// 씬 고유 ID
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// 씬 이름
    /// </summary>
    public string Name { get; set; } = "Untitled";

    /// <summary>
    /// 레이어 목록 (최대 11개: 배경 1 + 전경 10)
    /// </summary>
    public List<Layer> Layers { get; init; } = [];

    /// <summary>
    /// 파티클 이미터 목록
    /// </summary>
    public List<ParticleEmitter> ParticleEmitters { get; init; } = [];

    /// <summary>
    /// 씬 너비
    /// </summary>
    public int Width { get; set; } = 800;

    /// <summary>
    /// 씬 높이
    /// </summary>
    public int Height { get; set; } = 600;
}

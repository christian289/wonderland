using Wonderland.Domain.Entities;

namespace Wonderland.Application.Interfaces;

/// <summary>
/// 씬 저장소 인터페이스
/// </summary>
public interface ISceneRepository
{
    /// <summary>
    /// 씬 저장
    /// </summary>
    Task SaveAsync(Scene scene, string filePath);

    /// <summary>
    /// 씬 로드
    /// </summary>
    Task<Scene?> LoadAsync(string filePath);

    /// <summary>
    /// 파일 존재 여부 확인
    /// </summary>
    bool Exists(string filePath);
}

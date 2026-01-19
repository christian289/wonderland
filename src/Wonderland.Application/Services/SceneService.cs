using Wonderland.Application.Interfaces;
using Wonderland.Domain.Entities;
using Wonderland.Domain.Enums;
using Wonderland.Domain.ValueObjects;

namespace Wonderland.Application.Services;

/// <summary>
/// 씬 관리 서비스
/// </summary>
public sealed class SceneService(ISceneRepository repository)
{
    private Scene _currentScene = new();

    /// <summary>
    /// 현재 씬
    /// </summary>
    public Scene CurrentScene => _currentScene;

    /// <summary>
    /// 새 씬 생성
    /// </summary>
    public Scene CreateNew(string name, int width, int height)
    {
        _currentScene = new Scene
        {
            Name = name,
            Width = width,
            Height = height
        };
        return _currentScene;
    }

    /// <summary>
    /// 레이어 추가
    /// </summary>
    public Layer AddLayer(string name, string imagePath, int zIndex)
    {
        if (_currentScene.Layers.Count >= 11)
        {
            throw new InvalidOperationException("최대 레이어 수(11)를 초과할 수 없습니다.");
        }

        var layer = new Layer
        {
            Name = name,
            ImagePath = imagePath,
            ZIndex = zIndex,
            Parallax = new ParallaxSettings
            {
                DepthFactor = ParallaxCalculator.GetRecommendedDepthFactor(zIndex)
            }
        };

        _currentScene.Layers.Add(layer);
        return layer;
    }

    /// <summary>
    /// 레이어 제거
    /// </summary>
    public bool RemoveLayer(Guid layerId)
    {
        var layer = _currentScene.Layers.FirstOrDefault(l => l.Id == layerId);
        if (layer is null) return false;

        return _currentScene.Layers.Remove(layer);
    }

    /// <summary>
    /// 파티클 이미터 추가
    /// </summary>
    public ParticleEmitter AddParticleEmitter(ParticleType type)
    {
        var emitter = new ParticleEmitter
        {
            Type = type,
            Settings = GetDefaultParticleSettings(type)
        };

        _currentScene.ParticleEmitters.Add(emitter);
        return emitter;
    }

    /// <summary>
    /// 씬 저장
    /// </summary>
    public Task SaveAsync(string filePath) => repository.SaveAsync(_currentScene, filePath);

    /// <summary>
    /// 씬 로드
    /// </summary>
    public async Task<bool> LoadAsync(string filePath)
    {
        var scene = await repository.LoadAsync(filePath);
        if (scene is null) return false;

        _currentScene = scene;
        return true;
    }

    /// <summary>
    /// 파티클 타입별 기본 설정 반환
    /// </summary>
    private static ParticleSettings GetDefaultParticleSettings(ParticleType type) => type switch
    {
        ParticleType.Snow => new ParticleSettings
        {
            MaxParticles = 200,
            SpawnRate = 15,
            MinSize = 2,
            MaxSize = 6,
            MinSpeed = 30,
            MaxSpeed = 80,
            WindStrength = 10,
            Opacity = 0.7
        },
        ParticleType.Rain => new ParticleSettings
        {
            MaxParticles = 300,
            SpawnRate = 30,
            MinSize = 1,
            MaxSize = 3,
            MinSpeed = 200,
            MaxSpeed = 400,
            WindStrength = 20,
            Opacity = 0.5
        },
        _ => new ParticleSettings()
    };
}

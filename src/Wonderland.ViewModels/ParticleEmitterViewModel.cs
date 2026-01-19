using CommunityToolkit.Mvvm.ComponentModel;
using Wonderland.Domain.Entities;
using Wonderland.Domain.Enums;
using Wonderland.Domain.ValueObjects;

namespace Wonderland.ViewModels;

/// <summary>
/// 파티클 이미터 뷰모델
/// </summary>
public sealed partial class ParticleEmitterViewModel : ObservableObject
{
    private readonly ParticleEmitter _emitter;

    public ParticleEmitterViewModel(ParticleEmitter emitter)
    {
        _emitter = emitter;
    }

    /// <summary>
    /// 이미터 ID
    /// </summary>
    public Guid Id => _emitter.Id;

    /// <summary>
    /// 파티클 타입
    /// </summary>
    [ObservableProperty]
    private ParticleType _type = ParticleType.Snow;

    /// <summary>
    /// 활성화 여부
    /// </summary>
    [ObservableProperty]
    private bool _isEnabled = true;

    /// <summary>
    /// 최대 파티클 수
    /// </summary>
    [ObservableProperty]
    private int _maxParticles = 100;

    /// <summary>
    /// 생성 비율
    /// </summary>
    [ObservableProperty]
    private double _spawnRate = 10.0;

    /// <summary>
    /// 최소 크기
    /// </summary>
    [ObservableProperty]
    private double _minSize = 2.0;

    /// <summary>
    /// 최대 크기
    /// </summary>
    [ObservableProperty]
    private double _maxSize = 6.0;

    /// <summary>
    /// 최소 속도
    /// </summary>
    [ObservableProperty]
    private double _minSpeed = 50.0;

    /// <summary>
    /// 최대 속도
    /// </summary>
    [ObservableProperty]
    private double _maxSpeed = 150.0;

    /// <summary>
    /// 바람 강도
    /// </summary>
    [ObservableProperty]
    private double _windStrength;

    /// <summary>
    /// 투명도
    /// </summary>
    [ObservableProperty]
    private double _opacity = 0.8;

    /// <summary>
    /// 엔티티에서 ViewModel 로드
    /// </summary>
    public void LoadFromEntity()
    {
        Type = _emitter.Type;
        IsEnabled = _emitter.IsEnabled;
        MaxParticles = _emitter.Settings.MaxParticles;
        SpawnRate = _emitter.Settings.SpawnRate;
        MinSize = _emitter.Settings.MinSize;
        MaxSize = _emitter.Settings.MaxSize;
        MinSpeed = _emitter.Settings.MinSpeed;
        MaxSpeed = _emitter.Settings.MaxSpeed;
        WindStrength = _emitter.Settings.WindStrength;
        Opacity = _emitter.Settings.Opacity;
    }

    /// <summary>
    /// ViewModel에서 엔티티로 저장
    /// </summary>
    public void SaveToEntity()
    {
        _emitter.Type = Type;
        _emitter.IsEnabled = IsEnabled;
        _emitter.Settings = new ParticleSettings
        {
            MaxParticles = MaxParticles,
            SpawnRate = SpawnRate,
            MinSize = MinSize,
            MaxSize = MaxSize,
            MinSpeed = MinSpeed,
            MaxSpeed = MaxSpeed,
            WindStrength = WindStrength,
            Opacity = Opacity
        };
    }

    /// <summary>
    /// 내부 엔티티 반환
    /// </summary>
    public ParticleEmitter GetEntity() => _emitter;
}

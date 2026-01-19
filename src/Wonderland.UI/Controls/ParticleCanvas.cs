using System.Windows;
using System.Windows.Media;
using Wonderland.Domain.Enums;
using Wonderland.Domain.ValueObjects;

namespace Wonderland.UI.Controls;

/// <summary>
/// 파티클 렌더링 캔버스 (CompositionTarget.Rendering 기반)
/// </summary>
public sealed class ParticleCanvas : FrameworkElement
{
    /// <summary>
    /// 파티클 구조체
    /// </summary>
    private record struct Particle(
        double X,
        double Y,
        double Size,
        double SpeedY,
        double SpeedX,
        double Opacity,
        double Rotation);

    private readonly List<Particle> _particles = [];
    private readonly Random _random = new();
    private DateTime _lastUpdate;
    private double _spawnAccumulator;

    private ParticleType _particleType = ParticleType.Snow;
    private ParticleSettings _settings = new();
    private bool _isActive = true;

    // 캐시된 브러시
    private SolidColorBrush? _particleBrush;
    private Pen? _rainPen;

    public ParticleCanvas()
    {
        _lastUpdate = DateTime.Now;
        CompositionTarget.Rendering += OnRendering;
        UpdateBrushes();
    }

    /// <summary>
    /// 파티클 타입
    /// </summary>
    public ParticleType ParticleType
    {
        get => _particleType;
        set
        {
            _particleType = value;
            UpdateBrushes();
        }
    }

    /// <summary>
    /// 파티클 설정
    /// </summary>
    public ParticleSettings Settings
    {
        get => _settings;
        set => _settings = value;
    }

    /// <summary>
    /// 파티클 활성화 여부
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            if (!value)
            {
                _particles.Clear();
                InvalidateVisual();
            }
        }
    }

    /// <summary>
    /// 브러시 업데이트
    /// </summary>
    private void UpdateBrushes()
    {
        var color = _particleType switch
        {
            ParticleType.Snow => Colors.White,
            ParticleType.Rain => Color.FromArgb(180, 200, 220, 255),
            _ => Colors.White
        };

        _particleBrush = new SolidColorBrush(color);
        _particleBrush.Freeze();

        if (_particleType == ParticleType.Rain)
        {
            _rainPen = new Pen(_particleBrush, 1.5);
            _rainPen.Freeze();
        }
    }

    /// <summary>
    /// 렌더링 콜백
    /// </summary>
    private void OnRendering(object? sender, EventArgs e)
    {
        if (!_isActive || ActualWidth <= 0 || ActualHeight <= 0)
            return;

        var now = DateTime.Now;
        var deltaTime = (now - _lastUpdate).TotalSeconds;
        _lastUpdate = now;

        // deltaTime이 너무 크면 스킵 (창이 최소화되었다가 복귀 등)
        if (deltaTime > 0.5)
        {
            deltaTime = 0.016; // ~60fps
        }

        SpawnParticles(deltaTime);
        UpdateParticles(deltaTime);
        InvalidateVisual();
    }

    /// <summary>
    /// 파티클 생성
    /// </summary>
    private void SpawnParticles(double deltaTime)
    {
        if (_particles.Count >= _settings.MaxParticles)
            return;

        _spawnAccumulator += _settings.SpawnRate * deltaTime;

        while (_spawnAccumulator >= 1.0 && _particles.Count < _settings.MaxParticles)
        {
            _spawnAccumulator -= 1.0;

            var size = _settings.MinSize + _random.NextDouble() * (_settings.MaxSize - _settings.MinSize);
            var speed = _settings.MinSpeed + _random.NextDouble() * (_settings.MaxSpeed - _settings.MinSpeed);
            var windEffect = (_random.NextDouble() - 0.5) * 2 * _settings.WindStrength;

            var particle = new Particle(
                X: _random.NextDouble() * (ActualWidth + 100) - 50,
                Y: -size * 2,
                Size: size,
                SpeedY: speed,
                SpeedX: windEffect,
                Opacity: _settings.Opacity * (0.7 + _random.NextDouble() * 0.3),
                Rotation: _random.NextDouble() * 360
            );

            _particles.Add(particle);
        }
    }

    /// <summary>
    /// 파티클 업데이트
    /// </summary>
    private void UpdateParticles(double deltaTime)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];

            var newY = p.Y + p.SpeedY * deltaTime;
            var newX = p.X + p.SpeedX * deltaTime;
            var newRotation = p.Rotation + deltaTime * 30; // 눈송이 회전

            // 화면 밖으로 나가면 제거
            if (newY > ActualHeight + 50 || newX < -100 || newX > ActualWidth + 100)
            {
                _particles.RemoveAt(i);
                continue;
            }

            _particles[i] = p with
            {
                X = newX,
                Y = newY,
                Rotation = newRotation
            };
        }
    }

    /// <summary>
    /// 렌더링
    /// </summary>
    protected override void OnRender(DrawingContext dc)
    {
        if (!_isActive || _particleBrush is null)
            return;

        foreach (var p in _particles)
        {
            var brush = _particleBrush.Clone();
            brush.Opacity = p.Opacity;
            brush.Freeze();

            if (_particleType == ParticleType.Rain && _rainPen is not null)
            {
                // 빗방울: 선으로 렌더링
                var rainLength = p.Size * 4;
                dc.DrawLine(_rainPen,
                    new Point(p.X, p.Y),
                    new Point(p.X + p.SpeedX * 0.02, p.Y + rainLength));
            }
            else
            {
                // 눈송이: 원으로 렌더링
                dc.DrawEllipse(brush, null, new Point(p.X, p.Y), p.Size, p.Size);
            }
        }
    }

    /// <summary>
    /// 모든 파티클 제거
    /// </summary>
    public void Clear()
    {
        _particles.Clear();
        InvalidateVisual();
    }
}

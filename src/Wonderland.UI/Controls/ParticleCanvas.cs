using System.Windows;
using System.Windows.Media;
using Wonderland.Domain.Enums;
using Wonderland.Domain.ValueObjects;

namespace Wonderland.UI.Controls;

/// <summary>
/// 파티클 렌더링 캔버스 (CompositionTarget.Rendering 기반)
/// Particle rendering canvas (CompositionTarget.Rendering based)
/// </summary>
public sealed class ParticleCanvas : FrameworkElement
{
    #region DependencyProperties

    /// <summary>
    /// ParticleType DependencyProperty
    /// </summary>
    public static readonly DependencyProperty ParticleTypeProperty =
        DependencyProperty.Register(
            nameof(ParticleType),
            typeof(ParticleType),
            typeof(ParticleCanvas),
            new PropertyMetadata(ParticleType.Snow, OnParticleTypeChanged));

    /// <summary>
    /// Settings DependencyProperty
    /// </summary>
    public static readonly DependencyProperty SettingsProperty =
        DependencyProperty.Register(
            nameof(Settings),
            typeof(ParticleSettings),
            typeof(ParticleCanvas),
            new PropertyMetadata(new ParticleSettings()));

    /// <summary>
    /// IsActive DependencyProperty
    /// </summary>
    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(ParticleCanvas),
            new PropertyMetadata(true, OnIsActiveChanged));

    private static void OnParticleTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ParticleCanvas canvas)
        {
            canvas.UpdateBrushes();
        }
    }

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ParticleCanvas canvas && e.NewValue is false)
        {
            canvas._particles.Clear();
            canvas.InvalidateVisual();
        }
    }

    #endregion

    /// <summary>
    /// 파티클 구조체
    /// Particle structure
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

    // 캐시된 브러시
    // Cached brushes
    private SolidColorBrush? _particleBrush;
    private Pen? _rainPen;

    // Opacity별 캐시된 브러시 (0.0 ~ 1.0, 0.1 단위 = 11개)
    // Cached brushes by opacity (0.0 ~ 1.0, 0.1 step = 11 brushes)
    private readonly Dictionary<int, SolidColorBrush> _opacityBrushCache = new(11);

    public ParticleCanvas()
    {
        _lastUpdate = DateTime.Now;
        CompositionTarget.Rendering += OnRendering;
        UpdateBrushes();
    }

    /// <summary>
    /// 파티클 타입
    /// Particle type
    /// </summary>
    public ParticleType ParticleType
    {
        get => (ParticleType)GetValue(ParticleTypeProperty);
        set => SetValue(ParticleTypeProperty, value);
    }

    /// <summary>
    /// 파티클 설정
    /// Particle settings
    /// </summary>
    public ParticleSettings Settings
    {
        get => (ParticleSettings)GetValue(SettingsProperty);
        set => SetValue(SettingsProperty, value);
    }

    /// <summary>
    /// 파티클 활성화 여부
    /// Particle activation state
    /// </summary>
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <summary>
    /// 브러시 업데이트 및 Opacity 캐시 재구축
    /// Update brushes and rebuild opacity cache
    /// </summary>
    private void UpdateBrushes()
    {
        var particleType = ParticleType;
        var color = particleType switch
        {
            ParticleType.Snow => Colors.White,
            ParticleType.Rain => Color.FromArgb(180, 200, 220, 255),
            _ => Colors.White
        };

        _particleBrush = new SolidColorBrush(color);
        _particleBrush.Freeze();

        // Opacity 캐시 재구축 (0.0 ~ 1.0, 0.1 단위)
        // Rebuild opacity cache (0.0 ~ 1.0, 0.1 step)
        _opacityBrushCache.Clear();
        for (int i = 0; i <= 10; i++)
        {
            var opacity = i / 10.0;
            var brush = new SolidColorBrush(color) { Opacity = opacity };
            brush.Freeze();
            _opacityBrushCache[i] = brush;
        }

        if (particleType == ParticleType.Rain)
        {
            _rainPen = new Pen(_particleBrush, 1.5);
            _rainPen.Freeze();
        }
    }

    /// <summary>
    /// Opacity에 해당하는 캐시된 브러시 반환 (0.1 단위로 양자화)
    /// Get cached brush for opacity (quantized to 0.1 steps)
    /// </summary>
    private SolidColorBrush GetCachedBrush(double opacity)
    {
        // 0.1 단위로 양자화 (반올림)
        // Quantize to 0.1 steps (rounded)
        var key = (int)Math.Round(opacity * 10);
        key = Math.Clamp(key, 0, 10);

        return _opacityBrushCache.TryGetValue(key, out var brush)
            ? brush
            : _particleBrush!;
    }

    /// <summary>
    /// 렌더링 콜백
    /// Rendering callback
    /// </summary>
    private void OnRendering(object? sender, EventArgs e)
    {
        if (!IsActive || ActualWidth <= 0 || ActualHeight <= 0)
            return;

        var now = DateTime.Now;
        var deltaTime = (now - _lastUpdate).TotalSeconds;
        _lastUpdate = now;

        // deltaTime이 너무 크면 스킵 (창이 최소화되었다가 복귀 등)
        // Skip if deltaTime is too large (e.g., window was minimized)
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
    /// Spawn particles
    /// </summary>
    private void SpawnParticles(double deltaTime)
    {
        var settings = Settings;
        if (_particles.Count >= settings.MaxParticles)
            return;

        _spawnAccumulator += settings.SpawnRate * deltaTime;

        while (_spawnAccumulator >= 1.0 && _particles.Count < settings.MaxParticles)
        {
            _spawnAccumulator -= 1.0;

            var size = settings.MinSize + _random.NextDouble() * (settings.MaxSize - settings.MinSize);
            var speed = settings.MinSpeed + _random.NextDouble() * (settings.MaxSpeed - settings.MinSpeed);
            var windEffect = (_random.NextDouble() - 0.5) * 2 * settings.WindStrength;

            var particle = new Particle(
                X: _random.NextDouble() * (ActualWidth + 100) - 50,
                Y: -size * 2,
                Size: size,
                SpeedY: speed,
                SpeedX: windEffect,
                Opacity: settings.Opacity * (0.7 + _random.NextDouble() * 0.3),
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
    /// Rendering
    /// </summary>
    protected override void OnRender(DrawingContext dc)
    {
        if (!IsActive || _particleBrush is null)
            return;

        var particleType = ParticleType;

        foreach (var p in _particles)
        {
            // 캐시된 브러시 사용 (Clone 제거로 GC 부하 감소)
            // Use cached brush (reduced GC pressure by removing Clone)
            var brush = GetCachedBrush(p.Opacity);

            if (particleType == ParticleType.Rain && _rainPen is not null)
            {
                // 빗방울: 선으로 렌더링
                // Rain: render as lines
                var rainLength = p.Size * 4;
                dc.DrawLine(_rainPen,
                    new Point(p.X, p.Y),
                    new Point(p.X + p.SpeedX * 0.02, p.Y + rainLength));
            }
            else
            {
                // 눈송이: 원으로 렌더링
                // Snow: render as circles
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

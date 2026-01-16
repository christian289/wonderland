namespace Wonderland.Application.Settings;

/// <summary>
/// 애플리케이션 설정
/// Application settings
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// 창 설정
    /// Window settings
    /// </summary>
    public WindowSettings Window { get; set; } = new();

    /// <summary>
    /// 배경 이미지 경로 (Z-Index 0, 단일)
    /// Background image path (Z-Index 0, single)
    /// </summary>
    public string? BackgroundImagePath { get; set; }

    /// <summary>
    /// 전경 레이어 설정 목록 (Z-Index 1~10)
    /// Foreground layer settings list (Z-Index 1~10)
    /// </summary>
    public List<LayerSettings> Layers { get; set; } = [];

    /// <summary>
    /// 파티클 프리셋 (단일, Z-Index 1~11)
    /// Particle preset (single, Z-Index 1~11)
    /// </summary>
    public ParticlePresetSettings? ParticlePreset { get; set; }
}

/// <summary>
/// 창 설정
/// Window settings
/// </summary>
public sealed class WindowSettings
{
    /// <summary>
    /// 창 너비
    /// Window width
    /// </summary>
    public double Width { get; set; } = 300;

    /// <summary>
    /// 창 높이
    /// Window height
    /// </summary>
    public double Height { get; set; } = 600;

    /// <summary>
    /// 창 X 좌표
    /// Window X position
    /// </summary>
    public double Left { get; set; } = double.NaN;

    /// <summary>
    /// 창 Y 좌표
    /// Window Y position
    /// </summary>
    public double Top { get; set; } = double.NaN;
}

/// <summary>
/// 레이어 설정
/// Layer settings
/// </summary>
public sealed class LayerSettings
{
    /// <summary>
    /// 이미지 파일 경로
    /// Image file path
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// 레이어 이름
    /// Layer name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Z 인덱스 (레이어 순서)
    /// Z index (layer order)
    /// </summary>
    public int ZIndex { get; set; }

    /// <summary>
    /// 깊이 계수 (Parallax 효과 강도)
    /// Depth factor (Parallax effect strength)
    /// </summary>
    public double DepthFactor { get; set; } = 0.5;

    /// <summary>
    /// 최대 X 오프셋
    /// Maximum X offset
    /// </summary>
    public double MaxOffsetX { get; set; } = 50;

    /// <summary>
    /// 최대 Y 오프셋
    /// Maximum Y offset
    /// </summary>
    public double MaxOffsetY { get; set; } = 30;
}

/// <summary>
/// 파티클 프리셋 설정 (단일)
/// Particle preset settings (single)
/// </summary>
public sealed class ParticlePresetSettings
{
    /// <summary>
    /// 파티클 타입 (Snow, Rain, None)
    /// Particle type (Snow, Rain, None)
    /// </summary>
    public string Type { get; set; } = "None";

    /// <summary>
    /// Z-Index (1~11, 파티클이 그려지는 레이어 순서)
    /// Z-Index (1~11, layer order where particles are drawn)
    /// </summary>
    public int ZIndex { get; set; } = 11;

    /// <summary>
    /// 최대 파티클 수
    /// Maximum particle count
    /// </summary>
    public int MaxParticles { get; set; } = 200;

    /// <summary>
    /// 불투명도
    /// Opacity
    /// </summary>
    public double Opacity { get; set; } = 0.8;
}

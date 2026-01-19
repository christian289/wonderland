namespace Wonderland.Application.Settings;

/// <summary>
/// 애플리케이션 설정
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// 창 설정
    /// </summary>
    public WindowSettings Window { get; set; } = new();

    /// <summary>
    /// 배경 이미지 경로 (Z-Index 0, 단일)
    /// </summary>
    public string? BackgroundImagePath { get; set; }

    /// <summary>
    /// 전경 레이어 설정 목록 (Z-Index 1~10)
    /// </summary>
    public List<LayerSettings> Layers { get; set; } = [];

    /// <summary>
    /// 파티클 프리셋 (단일, Z-Index 1~11)
    /// </summary>
    public ParticlePresetSettings? ParticlePreset { get; set; }
}

/// <summary>
/// 창 설정
/// </summary>
public sealed class WindowSettings
{
    /// <summary>
    /// 창 너비
    /// </summary>
    public double Width { get; set; } = 300;

    /// <summary>
    /// 창 높이
    /// </summary>
    public double Height { get; set; } = 600;

    /// <summary>
    /// 창 X 좌표
    /// </summary>
    public double Left { get; set; } = double.NaN;

    /// <summary>
    /// 창 Y 좌표
    /// </summary>
    public double Top { get; set; } = double.NaN;
}

/// <summary>
/// 레이어 설정
/// </summary>
public sealed class LayerSettings
{
    /// <summary>
    /// 이미지 파일 경로
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// 레이어 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Z 인덱스 (레이어 순서)
    /// </summary>
    public int ZIndex { get; set; }

    /// <summary>
    /// 깊이 계수 (Parallax 효과 강도)
    /// </summary>
    public double DepthFactor { get; set; } = 0.5;

    /// <summary>
    /// 최대 X 오프셋
    /// </summary>
    public double MaxOffsetX { get; set; } = 50;

    /// <summary>
    /// 최대 Y 오프셋
    /// </summary>
    public double MaxOffsetY { get; set; } = 30;

    /// <summary>
    /// 레이어 X 좌표
    /// </summary>
    public double? X { get; set; }

    /// <summary>
    /// 레이어 Y 좌표
    /// </summary>
    public double? Y { get; set; }

    /// <summary>
    /// 레이어 너비
    /// </summary>
    public double? Width { get; set; }

    /// <summary>
    /// 레이어 높이
    /// </summary>
    public double? Height { get; set; }

    /// <summary>
    /// 레이어 회전 각도 (도)
    /// </summary>
    public double Rotation { get; set; }
}

/// <summary>
/// 파티클 프리셋 설정 (단일)
/// </summary>
public sealed class ParticlePresetSettings
{
    /// <summary>
    /// 파티클 타입 (Snow, Rain, None)
    /// </summary>
    public string Type { get; set; } = "None";

    /// <summary>
    /// Z-Index (1~11, 파티클이 그려지는 레이어 순서)
    /// </summary>
    public int ZIndex { get; set; } = 11;

    /// <summary>
    /// 최대 파티클 수
    /// </summary>
    public int MaxParticles { get; set; } = 200;

    /// <summary>
    /// 불투명도
    /// </summary>
    public double Opacity { get; set; } = 0.8;
}

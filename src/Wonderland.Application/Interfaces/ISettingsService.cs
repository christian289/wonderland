namespace Wonderland.Application.Interfaces;

using Wonderland.Application.Settings;

/// <summary>
/// 설정 저장/로드 서비스 인터페이스
/// Settings save/load service interface
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// 설정 저장
    /// Save settings
    /// </summary>
    Task SaveSettingsAsync(AppSettings settings);

    /// <summary>
    /// 설정 로드
    /// Load settings
    /// </summary>
    Task<AppSettings> LoadSettingsAsync();
}

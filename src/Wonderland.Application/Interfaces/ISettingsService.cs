namespace Wonderland.Application.Interfaces;

using Wonderland.Application.Settings;

/// <summary>
/// 설정 저장/로드 서비스 인터페이스
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// 설정 저장
    /// </summary>
    Task SaveSettingsAsync(AppSettings settings);

    /// <summary>
    /// 설정 로드
    /// </summary>
    Task<AppSettings> LoadSettingsAsync();
}

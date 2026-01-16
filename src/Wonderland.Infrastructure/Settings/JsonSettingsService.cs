namespace Wonderland.Infrastructure.Settings;

using System.Text.Json;
using System.Text.Json.Serialization;
using Wonderland.Application.Interfaces;
using Wonderland.Application.Settings;

/// <summary>
/// JSON 파일 기반 설정 서비스
/// JSON file-based settings service
/// </summary>
public sealed class JsonSettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public JsonSettingsService(string? basePath = null)
    {
        var appDirectory = basePath ?? AppDomain.CurrentDomain.BaseDirectory;
        _settingsPath = Path.Combine(appDirectory, "appsettings.json");
    }

    /// <summary>
    /// 설정 저장
    /// Save settings
    /// </summary>
    public async Task SaveSettingsAsync(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(_settingsPath, json);
    }

    /// <summary>
    /// 설정 로드
    /// Load settings
    /// </summary>
    public async Task<AppSettings> LoadSettingsAsync()
    {
        if (!File.Exists(_settingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }
}

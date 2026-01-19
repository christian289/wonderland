using System.Text.Json;
using System.Text.Json.Serialization;
using Wonderland.Application.Interfaces;
using Wonderland.Domain.Entities;

namespace Wonderland.Infrastructure.Persistence;

/// <summary>
/// JSON 파일 기반 씬 저장소
/// </summary>
public sealed class JsonSceneRepository : ISceneRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <inheritdoc />
    public async Task SaveAsync(Scene scene, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(scene, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <inheritdoc />
    public async Task<Scene?> LoadAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<Scene>(json, JsonOptions);
    }

    /// <inheritdoc />
    public bool Exists(string filePath) => File.Exists(filePath);
}

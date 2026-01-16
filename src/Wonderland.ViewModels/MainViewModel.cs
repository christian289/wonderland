using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wonderland.Application.Services;
using Wonderland.Domain.Entities;
using Wonderland.Domain.Enums;

namespace Wonderland.ViewModels;

/// <summary>
/// 메인 뷰모델
/// Main ViewModel
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly SceneService _sceneService;
    private readonly ParallaxCalculator _parallaxCalculator;

    public MainViewModel(SceneService sceneService, ParallaxCalculator parallaxCalculator)
    {
        _sceneService = sceneService;
        _parallaxCalculator = parallaxCalculator;
    }

    /// <summary>
    /// 현재 앱 모드
    /// Current app mode
    /// </summary>
    [ObservableProperty] private AppMode _currentMode = AppMode.Viewer;

    /// <summary>
    /// 씬 이름
    /// Scene name
    /// </summary>
    [ObservableProperty] private string _sceneName = "Untitled";

    /// <summary>
    /// 배경 이미지 경로 (Z-Index 0, 단일)
    /// Background image path (Z-Index 0, single)
    /// </summary>
    [ObservableProperty] private string? _backgroundImagePath;

    /// <summary>
    /// 배경 레이어 ViewModel
    /// Background layer ViewModel
    /// </summary>
    [ObservableProperty] private LayerViewModel? _backgroundLayer;

    /// <summary>
    /// 전경 레이어 목록 (Z-Index 1~10)
    /// Foreground layer list (Z-Index 1~10)
    /// </summary>
    public ObservableCollection<LayerViewModel> Layers { get; } = [];

    /// <summary>
    /// 파티클 프리셋 타입 (Snow, Rain, None)
    /// Particle preset type (Snow, Rain, None)
    /// </summary>
    [ObservableProperty] private ParticleType _presetType = ParticleType.None;

    /// <summary>
    /// 파티클 프리셋 Z-Index (1~11)
    /// Particle preset Z-Index (1~11)
    /// </summary>
    [ObservableProperty] private int _presetZIndex = 11;

    /// <summary>
    /// 파티클 최대 개수
    /// Particle max count
    /// </summary>
    [ObservableProperty] private int _presetMaxParticles = 200;

    /// <summary>
    /// 파티클 불투명도
    /// Particle opacity
    /// </summary>
    [ObservableProperty] private double _presetOpacity = 0.8;

    /// <summary>
    /// 선택된 레이어
    /// Selected layer
    /// </summary>
    [ObservableProperty] private LayerViewModel? _selectedLayer;

    /// <summary>
    /// 정규화된 마우스 X 좌표 (-1 ~ 1)
    /// Normalized mouse X coordinate (-1 ~ 1)
    /// </summary>
    [ObservableProperty] private double _normalizedMouseX;

    /// <summary>
    /// 정규화된 마우스 Y 좌표 (-1 ~ 1)
    /// Normalized mouse Y coordinate (-1 ~ 1)
    /// </summary>
    [ObservableProperty] private double _normalizedMouseY;

    /// <summary>
    /// 모드 전환
    /// Toggle mode
    /// </summary>
    [RelayCommand]
    private void ToggleMode()
    {
        CurrentMode = CurrentMode == AppMode.Viewer ? AppMode.Edit : AppMode.Viewer;
    }

    /// <summary>
    /// 새 씬 생성
    /// Create new scene
    /// </summary>
    [RelayCommand]
    private void NewScene()
    {
        _sceneService.CreateNew(SceneName, 800, 600);
        BackgroundImagePath = null;
        BackgroundLayer = null;
        Layers.Clear();
        PresetType = ParticleType.None;
    }

    /// <summary>
    /// 배경 이미지 설정 (Z-Index 0, 단일)
    /// Set background image (Z-Index 0, single)
    /// </summary>
    [RelayCommand]
    private void SetBackground(string imagePath)
    {
        BackgroundImagePath = imagePath;

        // 기존 배경 제거
        // Remove existing background
        if (BackgroundLayer is not null)
        {
            _sceneService.RemoveLayer(BackgroundLayer.Id);
        }

        // 새 배경 추가
        // Add new background
        var layer = _sceneService.AddLayer("Background", imagePath, 0);
        layer.Parallax = new Domain.ValueObjects.ParallaxSettings
        {
            DepthFactor = 0.1,
            MaxOffsetX = 20,
            MaxOffsetY = 10
        };

        BackgroundLayer = new LayerViewModel(layer);
        BackgroundLayer.LoadFromEntity();
    }

    /// <summary>
    /// 배경 제거
    /// Remove background
    /// </summary>
    [RelayCommand]
    private void RemoveBackground()
    {
        if (BackgroundLayer is not null)
        {
            _sceneService.RemoveLayer(BackgroundLayer.Id);
            BackgroundLayer = null;
            BackgroundImagePath = null;
        }
    }

    /// <summary>
    /// 전경 레이어 추가 (Z-Index 1~10)
    /// Add foreground layer (Z-Index 1~10)
    /// </summary>
    [RelayCommand]
    private void AddLayer(string imagePath)
    {
        if (Layers.Count >= 10) return;

        var zIndex = Layers.Count + 1;
        var layer = _sceneService.AddLayer($"Layer {zIndex}", imagePath, zIndex);
        var vm = new LayerViewModel(layer);
        vm.LoadFromEntity();
        Layers.Add(vm);
    }

    /// <summary>
    /// 레이어 제거
    /// Remove layer
    /// </summary>
    [RelayCommand]
    private void RemoveLayer(LayerViewModel? layer)
    {
        if (layer is null) return;

        _sceneService.RemoveLayer(layer.Id);
        Layers.Remove(layer);

        // Z-Index 재정렬
        // Reorder Z-Index
        for (var i = 0; i < Layers.Count; i++)
        {
            Layers[i].ZIndex = i + 1;
        }
    }

    /// <summary>
    /// 파티클 프리셋 설정 (단일)
    /// Set particle preset (single)
    /// </summary>
    [RelayCommand]
    private void SetPreset(ParticleType type)
    {
        PresetType = type;
    }

    /// <summary>
    /// 프리셋 Z-Index 변경
    /// Change preset Z-Index
    /// </summary>
    [RelayCommand]
    private void SetPresetZIndex(int zIndex)
    {
        PresetZIndex = Math.Clamp(zIndex, 1, 11);
    }

    /// <summary>
    /// 마우스 위치 업데이트 및 Parallax 계산
    /// Update mouse position and calculate parallax
    /// </summary>
    public void UpdateMousePosition(double normalizedX, double normalizedY)
    {
        NormalizedMouseX = normalizedX;
        NormalizedMouseY = normalizedY;

        // 배경 레이어 Parallax 계산
        // Calculate background layer parallax
        if (BackgroundLayer is not null)
        {
            var bgSettings = new Domain.ValueObjects.ParallaxSettings
            {
                DepthFactor = BackgroundLayer.DepthFactor,
                MaxOffsetX = BackgroundLayer.MaxOffsetX,
                MaxOffsetY = BackgroundLayer.MaxOffsetY,
                InvertX = BackgroundLayer.InvertX,
                InvertY = BackgroundLayer.InvertY
            };

            var (bgOffsetX, bgOffsetY) = _parallaxCalculator.CalculateOffset(
                normalizedX, normalizedY, bgSettings);

            BackgroundLayer.CurrentOffsetX = bgOffsetX;
            BackgroundLayer.CurrentOffsetY = bgOffsetY;
        }

        // 전경 레이어 Parallax 계산
        // Calculate foreground layers parallax
        foreach (var layer in Layers)
        {
            var settings = new Domain.ValueObjects.ParallaxSettings
            {
                DepthFactor = layer.DepthFactor,
                MaxOffsetX = layer.MaxOffsetX,
                MaxOffsetY = layer.MaxOffsetY,
                InvertX = layer.InvertX,
                InvertY = layer.InvertY
            };

            var (offsetX, offsetY) = _parallaxCalculator.CalculateOffset(
                normalizedX, normalizedY, settings);

            layer.CurrentOffsetX = offsetX;
            layer.CurrentOffsetY = offsetY;
        }
    }

    /// <summary>
    /// 모든 레이어 가져오기 (배경 + 전경)
    /// Get all layers (background + foreground)
    /// </summary>
    public IEnumerable<LayerViewModel> GetAllLayers()
    {
        if (BackgroundLayer is not null)
        {
            yield return BackgroundLayer;
        }

        foreach (var layer in Layers.OrderBy(l => l.ZIndex))
        {
            yield return layer;
        }
    }
}

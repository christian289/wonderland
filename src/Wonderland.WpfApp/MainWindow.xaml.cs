using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using DragEventArgs = System.Windows.DragEventArgs;
using DataObject = System.Windows.DataObject;
using DragDropEffects = System.Windows.DragDropEffects;
using Point = System.Windows.Point;
using Cursors = System.Windows.Input.Cursors;
using Wonderland.Application.Interfaces;
using Wonderland.Application.Settings;
using Wonderland.Domain.Enums;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Wonderland.ViewModels;
using Wonderland.WpfApp.Services.Editing;
using Wonderland.WpfServices;

namespace Wonderland.WpfApp;

/// <summary>
/// MainWindow 코드비하인드
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly WindowModeService _windowModeService;
    private readonly MouseTrackingService _mouseTrackingService;
    private readonly GlobalMouseHook _globalMouseHook;
    private readonly GlobalKeyboardHook _globalKeyboardHook;
    private readonly TrayIconService _trayIconService;
    private readonly ISettingsService _settingsService;

    // 편집 서비스
    private UndoService? _undoService;
    private LayerManipulationService? _layerManipulationService;
    private SelectionIndicatorService? _selectionIndicatorService;

    // 에디터 패널 너비
    private const double EditorPanelWidth = 200;

    // 원래 창 너비 (Edit 모드 전환 시 복원용)
    private double _originalWindowWidth;

    // 레이어 드래그 앤 드롭용 필드
    private Point _layerDragStartPoint;
    private bool _isLayerDragging;

    public MainWindow(
        MainViewModel viewModel,
        WindowModeService windowModeService,
        MouseTrackingService mouseTrackingService,
        GlobalMouseHook globalMouseHook,
        GlobalKeyboardHook globalKeyboardHook,
        TrayIconService trayIconService,
        ISettingsService settingsService)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _windowModeService = windowModeService;
        _mouseTrackingService = mouseTrackingService;
        _globalMouseHook = globalMouseHook;
        _globalKeyboardHook = globalKeyboardHook;
        _trayIconService = trayIconService;
        _settingsService = settingsService;

        DataContext = _viewModel;

        SetupEventHandlers();
    }

    #region 이벤트 핸들러 설정

    /// <summary>
    /// 이벤트 핸들러 설정
    /// </summary>
    private void SetupEventHandlers()
    {
        // 창 이벤트
        Loaded += OnWindowLoaded;
        Closed += OnWindowClosed;
        SizeChanged += OnWindowSizeChanged;
        KeyDown += OnKeyDown;

        // 모드 변경
        _windowModeService.ModeChanged += OnModeChanged;

        // 전역 훅
        _globalMouseHook.MouseMoved += OnGlobalMouseMoved;
        _globalKeyboardHook.KeyPressed += OnGlobalKeyPressed;

        // 트레이 아이콘 이벤트
        _trayIconService.ExitRequested += OnExitRequested;
        _trayIconService.ModeToggleRequested += OnModeToggleRequested;
        _trayIconService.EditModeRequested += OnEditModeRequested;

        // 버튼 클릭
        SetBackgroundButton.Click += OnSetBackgroundClick;
        RemoveBackgroundButton.Click += OnRemoveBackgroundClick;
        AddLayerButton.Click += OnAddLayerClick;

        // 프리셋 라디오 버튼
        PresetNoneRadio.Checked += (_, _) => OnPresetChanged(ParticleType.None);
        PresetSnowRadio.Checked += (_, _) => OnPresetChanged(ParticleType.Snow);
        PresetRainRadio.Checked += (_, _) => OnPresetChanged(ParticleType.Rain);

        // 드래그 영역 이벤트
        DragArea.MouseLeftButtonDown += OnDragAreaMouseLeftButtonDown;

        // 레이어 리스트 드래그 앤 드롭
        LayerListBox.PreviewMouseLeftButtonDown += OnLayerListPreviewMouseDown;
        LayerListBox.PreviewMouseMove += OnLayerListPreviewMouseMove;
        LayerListBox.Drop += OnLayerListDrop;

        // 선택 오버레이 이벤트 (이미지 조작)
        SelectionOverlay.MouseLeftButtonDown += OnSelectionOverlayMouseDown;
        SelectionOverlay.MouseLeftButtonUp += OnSelectionOverlayMouseUp;
        SelectionOverlay.MouseMove += OnSelectionOverlayMouseMove;
    }

    #endregion

    #region 창 생명주기

    /// <summary>
    /// 창 로드 완료
    /// </summary>
    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        // 편집 서비스 초기화
        _undoService = new UndoService();
        _layerManipulationService = new LayerManipulationService(ParallaxCanvas, _undoService);
        _selectionIndicatorService = new SelectionIndicatorService(SelectionOverlay);

        _windowModeService.Initialize(this);
        _globalMouseHook.Start();
        _globalKeyboardHook.Start();
        _trayIconService.Initialize();

        // 설정 로드 (NewScene 호출하지 않음 - 설정이 초기화됨)
        await LoadSettingsAsync();

        _mouseTrackingService.Initialize(ActualWidth, ActualHeight);

        // 파티클 업데이트
        UpdateParticleCanvas();
        UpdateUI();

        // 시작 알림
        _trayIconService.ShowNotification("Wonderland", "Press F12 to toggle Edit mode");
    }

    /// <summary>
    /// 창 닫힘
    /// </summary>
    private async void OnWindowClosed(object? sender, EventArgs e)
    {
        await SaveSettingsAsync();

        _globalMouseHook.Stop();
        _globalMouseHook.Dispose();
        _globalKeyboardHook.Stop();
        _globalKeyboardHook.Dispose();
        _trayIconService.Dispose();
    }

    /// <summary>
    /// 창 크기 변경
    /// </summary>
    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _mouseTrackingService.UpdateWindowSize(e.NewSize.Width, e.NewSize.Height);
        ScaleBackgroundToWindow();

        if (_windowModeService.CurrentMode == AppMode.Edit)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, UpdateSelectionOverlaySize);
        }
    }

    #endregion

    #region 설정 로드/저장

    /// <summary>
    /// 설정 로드
    /// </summary>
    private async Task LoadSettingsAsync()
    {
        var settings = await _settingsService.LoadSettingsAsync();

        // 창 크기 적용
        Width = settings.Window.Width;
        Height = settings.Window.Height;

        // 창 위치 적용 (저장된 위치가 있는 경우)
        if (!double.IsNaN(settings.Window.Left) && !double.IsNaN(settings.Window.Top))
        {
            Left = settings.Window.Left;
            Top = settings.Window.Top;
            WindowStartupLocation = WindowStartupLocation.Manual;
        }

        // 배경 로드
        if (!string.IsNullOrEmpty(settings.BackgroundImagePath) && File.Exists(settings.BackgroundImagePath))
        {
            _viewModel.SetBackgroundCommand.Execute(settings.BackgroundImagePath);
            if (_viewModel.BackgroundLayer is not null)
            {
                ParallaxCanvas.AddLayer(_viewModel.BackgroundLayer.GetEntity());
            }
        }

        // 전경 레이어 로드
        foreach (var layerSetting in settings.Layers.OrderBy(l => l.ZIndex))
        {
            if (!File.Exists(layerSetting.ImagePath)) continue;

            _viewModel.AddLayerCommand.Execute(layerSetting.ImagePath);

            var layer = _viewModel.Layers.LastOrDefault();
            if (layer is null) continue;

            layer.ZIndex = layerSetting.ZIndex;
            layer.DepthFactor = layerSetting.DepthFactor;
            layer.MaxOffsetX = layerSetting.MaxOffsetX;
            layer.MaxOffsetY = layerSetting.MaxOffsetY;

            ParallaxCanvas.AddLayer(layer.GetEntity());

            // 레이어 변환 정보 적용 (저장된 위치가 있는 경우)
            if (layerSetting.X.HasValue && layerSetting.Y.HasValue &&
                layerSetting.Width.HasValue && layerSetting.Height.HasValue)
            {
                ParallaxCanvas.UpdateLayerTransform(
                    layer.Id,
                    layerSetting.X.Value,
                    layerSetting.Y.Value,
                    layerSetting.Width.Value,
                    layerSetting.Height.Value);
            }

            // 회전 정보 적용
            if (Math.Abs(layerSetting.Rotation) > 0.01)
            {
                ParallaxCanvas.UpdateLayerRotation(layer.Id, layerSetting.Rotation);
            }
        }

        // 파티클 프리셋 로드
        if (settings.ParticlePreset is not null)
        {
            var presetType = Enum.TryParse<ParticleType>(settings.ParticlePreset.Type, out var type)
                ? type
                : ParticleType.None;

            _viewModel.PresetType = presetType;
            _viewModel.PresetZIndex = settings.ParticlePreset.ZIndex;
            _viewModel.PresetMaxParticles = settings.ParticlePreset.MaxParticles;
            _viewModel.PresetOpacity = settings.ParticlePreset.Opacity;
        }

        // 배경 스케일 적용 (Dispatcher를 통해 레이아웃 업데이트 후 실행)
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, ScaleBackgroundToWindow);
    }

    /// <summary>
    /// 설정 저장
    /// </summary>
    private async Task SaveSettingsAsync()
    {
        var settings = new AppSettings
        {
            Window = new WindowSettings
            {
                Width = ActualWidth,
                Height = ActualHeight,
                Left = Left,
                Top = Top
            },
            BackgroundImagePath = _viewModel.BackgroundImagePath,
            Layers = _viewModel.Layers.Select(l =>
            {
                var bounds = ParallaxCanvas.GetLayerBounds(l.Id);
                var rotation = ParallaxCanvas.GetLayerRotation(l.Id);

                return new LayerSettings
                {
                    ImagePath = l.ImagePath,
                    Name = l.Name,
                    ZIndex = l.ZIndex,
                    DepthFactor = l.DepthFactor,
                    MaxOffsetX = l.MaxOffsetX,
                    MaxOffsetY = l.MaxOffsetY,
                    X = bounds?.X,
                    Y = bounds?.Y,
                    Width = bounds?.Width,
                    Height = bounds?.Height,
                    Rotation = rotation
                };
            }).ToList(),
            ParticlePreset = new ParticlePresetSettings
            {
                Type = _viewModel.PresetType.ToString(),
                ZIndex = _viewModel.PresetZIndex,
                MaxParticles = _viewModel.PresetMaxParticles,
                Opacity = _viewModel.PresetOpacity
            }
        };

        await _settingsService.SaveSettingsAsync(settings);
    }

    #endregion

    #region 키보드 입력

    /// <summary>
    /// 키보드 입력 처리
    /// </summary>
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F12)
        {
            _windowModeService.ToggleMode();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _windowModeService.CurrentMode == AppMode.Edit)
        {
            _windowModeService.SetMode(AppMode.Viewer);
            e.Handled = true;
        }
        else if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control &&
                 _windowModeService.CurrentMode == AppMode.Edit)
        {
            PerformUndo();
            e.Handled = true;
        }
        else if (e.Key == Key.Delete && _windowModeService.CurrentMode == AppMode.Edit)
        {
            DeleteSelectedLayer();
            e.Handled = true;
        }
    }

    /// <summary>
    /// 전역 키보드 입력 처리
    /// </summary>
    private void OnGlobalKeyPressed(object? sender, Key key)
    {
        if (key == Key.F12)
        {
            Dispatcher.Invoke(() => _windowModeService.ToggleMode());
        }
    }

    #endregion

    #region 모드 변경

    /// <summary>
    /// 모드 변경 처리
    /// </summary>
    private void OnModeChanged(object? sender, AppMode mode)
    {
        Dispatcher.Invoke(() =>
        {
            var isEditMode = mode == AppMode.Edit;

            if (isEditMode)
            {
                // Edit 모드 진입: 창 확장
                _originalWindowWidth = Width;
                Width = _originalWindowWidth + EditorPanelWidth;
                EditorColumn.Width = new GridLength(EditorPanelWidth);
                EditorPanel.Visibility = Visibility.Visible;
                DragArea.Visibility = Visibility.Visible;

                UpdateUI();

                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, UpdateSelectionOverlaySize);
            }
            else
            {
                // Edit 모드 종료: 창 복원
                EditorColumn.Width = new GridLength(0);
                EditorPanel.Visibility = Visibility.Collapsed;
                DragArea.Visibility = Visibility.Collapsed;
                Width = _originalWindowWidth > 0 ? _originalWindowWidth : Width - EditorPanelWidth;
                ClearLayerSelection();

                // 설정 자동 저장
                _ = SaveSettingsAsync();
            }

            SelectionOverlay.Visibility = isEditMode ? Visibility.Visible : Visibility.Collapsed;

            _viewModel.CurrentMode = mode;
            _trayIconService.UpdateMode(mode);
        });
    }

    /// <summary>
    /// SelectionOverlay 크기 업데이트
    /// </summary>
    private void UpdateSelectionOverlaySize()
    {
        SelectionOverlay.Width = ContentGrid.ActualWidth;
        SelectionOverlay.Height = ContentGrid.ActualHeight;
    }

    #endregion

    #region 트레이 아이콘 이벤트

    private void OnExitRequested(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown());
    }

    private void OnModeToggleRequested(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() => _windowModeService.ToggleMode());
    }

    private void OnEditModeRequested(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() => _windowModeService.SetMode(AppMode.Edit));
    }

    #endregion

    #region 마우스 이벤트

    /// <summary>
    /// 전역 마우스 이동 처리
    /// </summary>
    private void OnGlobalMouseMoved(object? sender, Point position)
    {
        Dispatcher.Invoke(() =>
        {
            // Edit 모드에서는 Parallax 효과 비활성화
            if (_windowModeService.CurrentMode == AppMode.Edit) return;

            var (normalizedX, normalizedY) = _mouseTrackingService.GetNormalizedPosition(position);
            _viewModel.UpdateMousePosition(normalizedX, normalizedY);

            var offsets = _viewModel.GetAllLayers().Select(l => (l.Id, l.CurrentOffsetX, l.CurrentOffsetY)).ToList();
            ParallaxCanvas.UpdateAllOffsets(offsets);
        });
    }

    /// <summary>
    /// 드래그 영역에서 마우스 왼쪽 버튼 클릭 처리 (창 드래그)
    /// </summary>
    private void OnDragAreaMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_windowModeService.CurrentMode == AppMode.Edit)
        {
            DragMove();
            e.Handled = true;
        }
    }

    #endregion

    #region 레이어 리스트 드래그 앤 드롭

    private void OnLayerListPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _layerDragStartPoint = e.GetPosition(null);
        _isLayerDragging = false;
    }

    private void OnLayerListPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var currentPos = e.GetPosition(null);
        var diff = _layerDragStartPoint - currentPos;

        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            if (!_isLayerDragging && LayerListBox.SelectedItem is not null)
            {
                _isLayerDragging = true;
                var dragData = new DataObject("LayerIndex", LayerListBox.SelectedIndex);
                DragDrop.DoDragDrop(LayerListBox, dragData, DragDropEffects.Move);
            }
        }
    }

    private void OnLayerListDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("LayerIndex")) return;

        var sourceIndex = (int)e.Data.GetData("LayerIndex")!;
        var targetIndex = GetDropTargetIndex(e);

        if (targetIndex < 0 || sourceIndex == targetIndex) return;

        ReorderLayer(sourceIndex, targetIndex);
        _isLayerDragging = false;
    }

    private int GetDropTargetIndex(DragEventArgs e)
    {
        var targetIndex = -1;
        var pos = e.GetPosition(LayerListBox);

        for (var i = 0; i < LayerListBox.Items.Count; i++)
        {
            if (LayerListBox.ItemContainerGenerator.ContainerFromIndex(i) is not ListBoxItem item) continue;

            var itemPos = item.TranslatePoint(new Point(0, 0), LayerListBox);
            var itemHeight = item.ActualHeight;

            if (pos.Y >= itemPos.Y && pos.Y < itemPos.Y + itemHeight)
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex < 0 && LayerListBox.Items.Count > 0)
        {
            targetIndex = LayerListBox.Items.Count - 1;
        }

        return targetIndex;
    }

    private void ReorderLayer(int sourceIndex, int targetIndex)
    {
        var layers = _viewModel.Layers.OrderBy(l => l.ZIndex).ToList();

        if (sourceIndex < 0 || sourceIndex >= layers.Count ||
            targetIndex < 0 || targetIndex >= layers.Count)
        {
            return;
        }

        var movedLayer = layers[sourceIndex];
        layers.RemoveAt(sourceIndex);
        layers.Insert(targetIndex, movedLayer);

        for (var i = 0; i < layers.Count; i++)
        {
            layers[i].ZIndex = i + 1;
            ParallaxCanvas.UpdateLayerZIndex(layers[i].Id, layers[i].ZIndex);
        }

        UpdateUI();
    }

    #endregion

    #region 배경/레이어 관리

    /// <summary>
    /// 배경 이미지를 콘텐츠 영역에 맞게 스케일
    /// </summary>
    private void ScaleBackgroundToWindow()
    {
        if (_viewModel.BackgroundLayer is null) return;

        var contentWidth = _windowModeService.CurrentMode == AppMode.Edit
            ? ActualWidth - EditorPanelWidth
            : ActualWidth;

        ParallaxCanvas.ScaleBackgroundToFit(_viewModel.BackgroundLayer.Id, contentWidth, ActualHeight);
    }

    private void OnSetBackgroundClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*",
            Title = "Select background image"
        };

        if (dialog.ShowDialog() != true) return;

        if (_viewModel.BackgroundLayer is not null)
        {
            ParallaxCanvas.RemoveLayer(_viewModel.BackgroundLayer.Id);
        }

        _viewModel.SetBackgroundCommand.Execute(dialog.FileName);

        if (_viewModel.BackgroundLayer is not null)
        {
            ParallaxCanvas.AddLayer(_viewModel.BackgroundLayer.GetEntity());
            ScaleBackgroundToWindow();
        }

        UpdateUI();
    }

    private void OnRemoveBackgroundClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.BackgroundLayer is not null)
        {
            ParallaxCanvas.RemoveLayer(_viewModel.BackgroundLayer.Id);
        }

        _viewModel.RemoveBackgroundCommand.Execute(null);
        UpdateUI();
    }

    private void OnAddLayerClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*",
            Title = "Select layer image"
        };

        if (dialog.ShowDialog() != true) return;

        _viewModel.AddLayerCommand.Execute(dialog.FileName);

        var layer = _viewModel.Layers.LastOrDefault();
        if (layer is not null)
        {
            ParallaxCanvas.AddLayer(layer.GetEntity());
        }

        UpdateUI();
    }

    #endregion

    #region 파티클 프리셋

    private void OnPresetChanged(ParticleType type)
    {
        _viewModel.SetPresetCommand.Execute(type);
        UpdateParticleCanvas();
    }

    private void UpdateParticleCanvas()
    {
        if (_viewModel.PresetType == ParticleType.None)
        {
            ParticleCanvas.IsActive = false;
        }
        else
        {
            ParticleCanvas.ParticleType = _viewModel.PresetType;
            ParticleCanvas.Settings = new Domain.ValueObjects.ParticleSettings
            {
                MaxParticles = _viewModel.PresetMaxParticles,
                SpawnRate = _viewModel.PresetType == ParticleType.Snow ? 15 : 30,
                MinSize = _viewModel.PresetType == ParticleType.Snow ? 2 : 1,
                MaxSize = _viewModel.PresetType == ParticleType.Snow ? 6 : 3,
                MinSpeed = _viewModel.PresetType == ParticleType.Snow ? 30 : 200,
                MaxSpeed = _viewModel.PresetType == ParticleType.Snow ? 80 : 400,
                WindStrength = _viewModel.PresetType == ParticleType.Snow ? 10 : 20,
                Opacity = _viewModel.PresetOpacity
            };
            ParticleCanvas.IsActive = true;
        }
    }

    #endregion

    #region UI 갱신

    private void UpdateUI()
    {
        BackgroundPathText.Text = string.IsNullOrEmpty(_viewModel.BackgroundImagePath)
            ? "(None)"
            : Path.GetFileName(_viewModel.BackgroundImagePath);

        LayerListBox.ItemsSource = null;
        LayerListBox.ItemsSource = _viewModel.Layers
            .OrderBy(l => l.ZIndex)
            .Select(l => $"Z:{l.ZIndex} - {Path.GetFileName(l.ImagePath)}")
            .ToList();

        PresetNoneRadio.IsChecked = _viewModel.PresetType == ParticleType.None;
        PresetSnowRadio.IsChecked = _viewModel.PresetType == ParticleType.Snow;
        PresetRainRadio.IsChecked = _viewModel.PresetType == ParticleType.Rain;

        PresetZIndexText.Text = _viewModel.PresetZIndex.ToString();
    }

    #endregion

    #region 레이어 선택 및 조작

    /// <summary>
    /// Undo 실행
    /// </summary>
    private void PerformUndo()
    {
        if (_undoService is null || !_undoService.CanUndo) return;

        _undoService.Undo();
        UpdateSelectionIndicators();
    }

    /// <summary>
    /// 선택된 레이어 삭제
    /// </summary>
    private void DeleteSelectedLayer()
    {
        if (_layerManipulationService is null || !_layerManipulationService.HasSelection) return;

        var layerId = _layerManipulationService.SelectedLayerId!.Value;

        if (ParallaxCanvas.IsBackgroundLayer(layerId))
        {
            ParallaxCanvas.RemoveLayer(layerId);
            _viewModel.RemoveBackgroundCommand.Execute(null);
        }
        else
        {
            ParallaxCanvas.RemoveLayer(layerId);

            var layerVm = _viewModel.Layers.FirstOrDefault(l => l.Id == layerId);
            if (layerVm is not null)
            {
                _viewModel.Layers.Remove(layerVm);
            }
        }

        ClearLayerSelection();
        UpdateUI();
    }

    /// <summary>
    /// 레이어 선택 해제
    /// </summary>
    private void ClearLayerSelection()
    {
        _layerManipulationService?.ClearSelection();
        _selectionIndicatorService?.ClearIndicators();
    }

    /// <summary>
    /// 선택 표시 업데이트
    /// </summary>
    private void UpdateSelectionIndicators()
    {
        if (_layerManipulationService is null || _selectionIndicatorService is null) return;
        if (!_layerManipulationService.HasSelection) return;

        var bounds = _layerManipulationService.GetSelectedLayerBounds();
        if (!bounds.HasValue) return;

        var rotation = _layerManipulationService.GetSelectedLayerRotation();
        _selectionIndicatorService.UpdateIndicators(bounds.Value, rotation);
    }

    #endregion

    #region 선택 오버레이 이벤트

    private void OnSelectionOverlayMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_windowModeService.CurrentMode != AppMode.Edit) return;
        if (_layerManipulationService is null || _selectionIndicatorService is null) return;

        var pos = e.GetPosition(ParallaxCanvas);
        var overlayPos = e.GetPosition(SelectionOverlay);

        // 회전 핸들 체크
        if (_selectionIndicatorService.IsPointOnRotationHandle(overlayPos) && _layerManipulationService.HasSelection)
        {
            _layerManipulationService.StartRotation(pos);
            SelectionOverlay.CaptureMouse();
            e.Handled = true;
            return;
        }

        // 리사이즈 핸들 체크
        var resizeDirection = _selectionIndicatorService.GetResizeDirection(overlayPos);
        if (resizeDirection != ResizeDirection.None && _layerManipulationService.HasSelection)
        {
            _layerManipulationService.StartResize(pos, resizeDirection);
            SelectionOverlay.CaptureMouse();
            e.Handled = true;
            return;
        }

        // 레이어 히트 테스트
        var hitLayerId = ParallaxCanvas.HitTestLayer(pos);

        // 배경 레이어는 선택 불가
        if (hitLayerId.HasValue && !ParallaxCanvas.IsBackgroundLayer(hitLayerId.Value))
        {
            _layerManipulationService.SelectLayer(hitLayerId.Value);
            _selectionIndicatorService.CreateIndicators();
            UpdateSelectionIndicators();

            _layerManipulationService.StartDrag(pos);
            SelectionOverlay.CaptureMouse();
        }
        else
        {
            ClearLayerSelection();
        }

        e.Handled = true;
    }

    private void OnSelectionOverlayMouseUp(object sender, MouseButtonEventArgs e)
    {
        _layerManipulationService?.EndAllManipulations();
        SelectionOverlay.ReleaseMouseCapture();
    }

    private void OnSelectionOverlayMouseMove(object sender, MouseEventArgs e)
    {
        if (_windowModeService.CurrentMode != AppMode.Edit) return;
        if (_layerManipulationService is null || _selectionIndicatorService is null) return;
        if (!_layerManipulationService.HasSelection) return;

        var currentPos = e.GetPosition(ParallaxCanvas);

        if (_layerManipulationService.IsRotating)
        {
            _layerManipulationService.UpdateRotation(currentPos);
            UpdateSelectionIndicators();
        }
        else if (_layerManipulationService.IsDragging)
        {
            _layerManipulationService.UpdateDrag(currentPos);
            UpdateSelectionIndicators();
        }
        else if (_layerManipulationService.IsResizing)
        {
            _layerManipulationService.UpdateResize(currentPos);
            UpdateSelectionIndicators();
        }
        else
        {
            // 커서 업데이트
            var overlayPos = e.GetPosition(SelectionOverlay);
            if (_selectionIndicatorService.IsPointOnRotationHandle(overlayPos))
            {
                SelectionOverlay.Cursor = Cursors.Hand;
            }
            else
            {
                var direction = _selectionIndicatorService.GetResizeDirection(overlayPos);
                SelectionOverlay.Cursor = SelectionIndicatorService.GetResizeCursor(direction);
            }
        }
    }

    #endregion
}

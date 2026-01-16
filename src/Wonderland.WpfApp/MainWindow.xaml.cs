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
using Cursor = System.Windows.Input.Cursor;
using Cursors = System.Windows.Input.Cursors;
using Wonderland.Application.Interfaces;
using Wonderland.Application.Settings;
using Wonderland.Domain.Enums;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Wonderland.ViewModels;
using Wonderland.WpfServices;

namespace Wonderland.WpfApp;

/// <summary>
/// MainWindow 코드비하인드
/// MainWindow code-behind
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

    /// <summary>
    /// 이벤트 핸들러 설정
    /// Setup event handlers
    /// </summary>
    private void SetupEventHandlers()
    {
        // 창 이벤트
        // Window events
        Loaded += OnWindowLoaded;
        Closed += OnWindowClosed;
        SizeChanged += OnWindowSizeChanged;
        KeyDown += OnKeyDown;

        // 모드 변경
        // Mode change
        _windowModeService.ModeChanged += OnModeChanged;

        // 전역 훅
        // Global hooks
        _globalMouseHook.MouseMoved += OnGlobalMouseMoved;
        _globalKeyboardHook.KeyPressed += OnGlobalKeyPressed;

        // 트레이 아이콘 이벤트
        // Tray icon events
        _trayIconService.ExitRequested += OnExitRequested;
        _trayIconService.ModeToggleRequested += OnModeToggleRequested;
        _trayIconService.EditModeRequested += OnEditModeRequested;

        // 버튼 클릭
        // Button clicks
        SetBackgroundButton.Click += OnSetBackgroundClick;
        RemoveBackgroundButton.Click += OnRemoveBackgroundClick;
        AddLayerButton.Click += OnAddLayerClick;

        // 프리셋 라디오 버튼
        // Preset radio buttons
        PresetNoneRadio.Checked += (_, _) => OnPresetChanged(ParticleType.None);
        PresetSnowRadio.Checked += (_, _) => OnPresetChanged(ParticleType.Snow);
        PresetRainRadio.Checked += (_, _) => OnPresetChanged(ParticleType.Rain);

        // 드래그 영역 이벤트
        // Drag area events
        DragArea.MouseLeftButtonDown += OnDragAreaMouseLeftButtonDown;

        // 레이어 리스트 드래그 앤 드롭
        // Layer list drag and drop
        LayerListBox.PreviewMouseLeftButtonDown += OnLayerListPreviewMouseDown;
        LayerListBox.PreviewMouseMove += OnLayerListPreviewMouseMove;
        LayerListBox.Drop += OnLayerListDrop;

        // 선택 오버레이 이벤트 (이미지 조작)
        // Selection overlay events (image manipulation)
        SelectionOverlay.MouseLeftButtonDown += OnSelectionOverlayMouseDown;
        SelectionOverlay.MouseLeftButtonUp += OnSelectionOverlayMouseUp;
        SelectionOverlay.MouseMove += OnSelectionOverlayMouseMove;
    }

    /// <summary>
    /// 창 로드 완료
    /// Window loaded
    /// </summary>
    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        _windowModeService.Initialize(this);
        _globalMouseHook.Start();
        _globalKeyboardHook.Start();
        _trayIconService.Initialize();

        // 설정 로드 (NewScene 호출하지 않음 - 설정이 초기화됨)
        // Load settings (don't call NewScene - it clears settings)
        await LoadSettingsAsync();

        _mouseTrackingService.Initialize(ActualWidth, ActualHeight);

        // 파티클 업데이트
        // Update particles
        UpdateParticleCanvas();
        UpdateUI();

        // 시작 알림
        // Startup notification
        _trayIconService.ShowNotification("Wonderland", "Press F12 to toggle Edit mode");
    }

    /// <summary>
    /// 설정 로드
    /// Load settings
    /// </summary>
    private async Task LoadSettingsAsync()
    {
        var settings = await _settingsService.LoadSettingsAsync();

        // 창 크기 적용
        // Apply window size
        Width = settings.Window.Width;
        Height = settings.Window.Height;

        // 창 위치 적용 (저장된 위치가 있는 경우)
        // Apply window position (if saved position exists)
        if (!double.IsNaN(settings.Window.Left) && !double.IsNaN(settings.Window.Top))
        {
            Left = settings.Window.Left;
            Top = settings.Window.Top;
            WindowStartupLocation = WindowStartupLocation.Manual;
        }

        // 배경 로드
        // Load background
        if (!string.IsNullOrEmpty(settings.BackgroundImagePath) && File.Exists(settings.BackgroundImagePath))
        {
            _viewModel.SetBackgroundCommand.Execute(settings.BackgroundImagePath);
            if (_viewModel.BackgroundLayer is not null)
            {
                ParallaxCanvas.AddLayer(_viewModel.BackgroundLayer.GetEntity());
            }
        }

        // 전경 레이어 로드
        // Load foreground layers
        foreach (var layerSetting in settings.Layers.OrderBy(l => l.ZIndex))
        {
            if (File.Exists(layerSetting.ImagePath))
            {
                _viewModel.AddLayerCommand.Execute(layerSetting.ImagePath);

                var layer = _viewModel.Layers.LastOrDefault();
                if (layer is not null)
                {
                    layer.ZIndex = layerSetting.ZIndex;
                    layer.DepthFactor = layerSetting.DepthFactor;
                    layer.MaxOffsetX = layerSetting.MaxOffsetX;
                    layer.MaxOffsetY = layerSetting.MaxOffsetY;

                    ParallaxCanvas.AddLayer(layer.GetEntity());
                }
            }
        }

        // 파티클 프리셋 로드
        // Load particle preset
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
        // Apply background scale (run after layout update via Dispatcher)
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
        {
            ScaleBackgroundToWindow();
        });
    }

    /// <summary>
    /// 창 닫힘
    /// Window closed
    /// </summary>
    private async void OnWindowClosed(object? sender, EventArgs e)
    {
        // 설정 저장
        // Save settings
        await SaveSettingsAsync();

        _globalMouseHook.Stop();
        _globalMouseHook.Dispose();
        _globalKeyboardHook.Stop();
        _globalKeyboardHook.Dispose();
        _trayIconService.Dispose();
    }

    /// <summary>
    /// 설정 저장
    /// Save settings
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
            Layers = _viewModel.Layers.Select(l => new LayerSettings
            {
                ImagePath = l.ImagePath,
                Name = l.Name,
                ZIndex = l.ZIndex,
                DepthFactor = l.DepthFactor,
                MaxOffsetX = l.MaxOffsetX,
                MaxOffsetY = l.MaxOffsetY
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

    /// <summary>
    /// 창 크기 변경
    /// Window size changed
    /// </summary>
    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _mouseTrackingService.UpdateWindowSize(e.NewSize.Width, e.NewSize.Height);

        // 배경 이미지 스케일 조정 (Edit 모드가 아닐 때만 콘텐츠 영역 크기 사용)
        // Scale background image (use content area size when not in Edit mode)
        ScaleBackgroundToWindow();

        // Edit 모드에서 SelectionOverlay 크기 업데이트
        // Update SelectionOverlay size in Edit mode
        if (_windowModeService.CurrentMode == AppMode.Edit)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
            {
                UpdateSelectionOverlaySize();
            });
        }
    }

    /// <summary>
    /// 배경 이미지를 콘텐츠 영역에 맞게 스케일
    /// Scale background image to fit content area
    /// </summary>
    private void ScaleBackgroundToWindow()
    {
        if (_viewModel.BackgroundLayer is null)
        {
            return;
        }

        // Edit 모드일 때는 에디터 패널 너비를 제외한 콘텐츠 영역 사용
        // When in Edit mode, use content area excluding editor panel width
        var contentWidth = _windowModeService.CurrentMode == AppMode.Edit
            ? ActualWidth - EditorPanelWidth
            : ActualWidth;

        ParallaxCanvas.ScaleBackgroundToFit(
            _viewModel.BackgroundLayer.Id,
            contentWidth,
            ActualHeight);
    }

    /// <summary>
    /// 키보드 입력 처리
    /// Handle keyboard input
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
    }

    /// <summary>
    /// 드래그 영역에서 마우스 왼쪽 버튼 클릭 처리 (창 드래그)
    /// Handle mouse left button down on drag area (window drag)
    /// </summary>
    private void OnDragAreaMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_windowModeService.CurrentMode == AppMode.Edit)
        {
            DragMove();
            e.Handled = true;
        }
    }

    // 레이어 드래그 앤 드롭용 필드
    // Fields for layer drag and drop
    private Point _layerDragStartPoint;
    private bool _isLayerDragging;

    /// <summary>
    /// 레이어 리스트 마우스 다운 처리
    /// Handle layer list mouse down
    /// </summary>
    private void OnLayerListPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _layerDragStartPoint = e.GetPosition(null);
        _isLayerDragging = false;
    }

    /// <summary>
    /// 레이어 리스트 마우스 이동 처리
    /// Handle layer list mouse move
    /// </summary>
    private void OnLayerListPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

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

    /// <summary>
    /// 레이어 리스트 드롭 처리
    /// Handle layer list drop
    /// </summary>
    private void OnLayerListDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("LayerIndex"))
        {
            return;
        }

        var sourceIndex = (int)e.Data.GetData("LayerIndex")!;

        // 드롭 위치 계산
        // Calculate drop position
        var targetIndex = GetDropTargetIndex(e);
        if (targetIndex < 0 || sourceIndex == targetIndex)
        {
            return;
        }

        // 레이어 순서 변경
        // Reorder layers
        ReorderLayer(sourceIndex, targetIndex);
        _isLayerDragging = false;
    }

    /// <summary>
    /// 드롭 대상 인덱스 계산
    /// Calculate drop target index
    /// </summary>
    private int GetDropTargetIndex(DragEventArgs e)
    {
        var targetIndex = -1;
        var pos = e.GetPosition(LayerListBox);

        for (var i = 0; i < LayerListBox.Items.Count; i++)
        {
            var item = LayerListBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
            if (item is null)
            {
                continue;
            }

            var itemPos = item.TranslatePoint(new Point(0, 0), LayerListBox);
            var itemHeight = item.ActualHeight;

            if (pos.Y >= itemPos.Y && pos.Y < itemPos.Y + itemHeight)
            {
                targetIndex = i;
                break;
            }
        }

        // 마지막 항목 아래에 드롭한 경우
        // Dropped below the last item
        if (targetIndex < 0 && LayerListBox.Items.Count > 0)
        {
            targetIndex = LayerListBox.Items.Count - 1;
        }

        return targetIndex;
    }

    /// <summary>
    /// 레이어 순서 변경
    /// Reorder layer
    /// </summary>
    private void ReorderLayer(int sourceIndex, int targetIndex)
    {
        var layers = _viewModel.Layers.OrderBy(l => l.ZIndex).ToList();

        if (sourceIndex < 0 || sourceIndex >= layers.Count ||
            targetIndex < 0 || targetIndex >= layers.Count)
        {
            return;
        }

        // Z-Index 재할당 (1부터 시작, 0은 배경용)
        // Reassign Z-Index (starting from 1, 0 is for background)
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

    /// <summary>
    /// 전역 키보드 입력 처리
    /// Handle global key press
    /// </summary>
    private void OnGlobalKeyPressed(object? sender, Key key)
    {
        if (key == Key.F12)
        {
            Dispatcher.Invoke(() =>
            {
                _windowModeService.ToggleMode();
            });
        }
    }

    // 에디터 패널 너비
    // Editor panel width
    private const double EditorPanelWidth = 200;

    // 원래 창 너비 (Edit 모드 전환 시 복원용)
    // Original window width (for restoration when exiting Edit mode)
    private double _originalWindowWidth;

    /// <summary>
    /// 모드 변경 처리
    /// Handle mode change
    /// </summary>
    private void OnModeChanged(object? sender, AppMode mode)
    {
        Dispatcher.Invoke(() =>
        {
            var isEditMode = mode == AppMode.Edit;

            if (isEditMode)
            {
                // Edit 모드 진입: 창 확장
                // Entering Edit mode: expand window
                _originalWindowWidth = Width;
                Width = _originalWindowWidth + EditorPanelWidth;
                EditorColumn.Width = new GridLength(EditorPanelWidth);
                EditorPanel.Visibility = Visibility.Visible;
                DragArea.Visibility = Visibility.Visible;

                // UI 갱신
                // Update UI
                UpdateUI();

                // SelectionOverlay 크기 설정 (레이아웃 업데이트 후 실행)
                // Set SelectionOverlay size (after layout update)
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
                {
                    UpdateSelectionOverlaySize();
                });
            }
            else
            {
                // Edit 모드 종료: 창 복원
                // Exiting Edit mode: restore window
                EditorColumn.Width = new GridLength(0);
                EditorPanel.Visibility = Visibility.Collapsed;
                DragArea.Visibility = Visibility.Collapsed;
                Width = _originalWindowWidth > 0 ? _originalWindowWidth : Width - EditorPanelWidth;
                ClearLayerSelection();
            }

            SelectionOverlay.Visibility = isEditMode
                ? Visibility.Visible
                : Visibility.Collapsed;

            _viewModel.CurrentMode = mode;
            _trayIconService.UpdateMode(mode);
        });
    }

    /// <summary>
    /// SelectionOverlay 크기 업데이트
    /// Update SelectionOverlay size
    /// </summary>
    private void UpdateSelectionOverlaySize()
    {
        // ContentGrid의 실제 크기로 설정
        // Set to ContentGrid's actual size
        SelectionOverlay.Width = ContentGrid.ActualWidth;
        SelectionOverlay.Height = ContentGrid.ActualHeight;
    }

    /// <summary>
    /// 트레이 아이콘에서 종료 요청
    /// Exit requested from tray icon
    /// </summary>
    private void OnExitRequested(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            System.Windows.Application.Current.Shutdown();
        });
    }

    /// <summary>
    /// 트레이 아이콘에서 모드 전환 요청
    /// Mode toggle requested from tray icon
    /// </summary>
    private void OnModeToggleRequested(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            _windowModeService.ToggleMode();
        });
    }

    /// <summary>
    /// 트레이 아이콘에서 Edit 모드 요청
    /// Edit mode requested from tray icon
    /// </summary>
    private void OnEditModeRequested(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            _windowModeService.SetMode(AppMode.Edit);
        });
    }

    /// <summary>
    /// 전역 마우스 이동 처리
    /// Handle global mouse move
    /// </summary>
    private void OnGlobalMouseMoved(object? sender, Point position)
    {
        Dispatcher.Invoke(() =>
        {
            // Edit 모드에서는 Parallax 효과 비활성화
            // Disable parallax effect in Edit mode
            if (_windowModeService.CurrentMode == AppMode.Edit)
            {
                return;
            }

            var (normalizedX, normalizedY) = _mouseTrackingService.GetNormalizedPosition(position);
            _viewModel.UpdateMousePosition(normalizedX, normalizedY);

            // ParallaxCanvas 업데이트
            // Update ParallaxCanvas
            var offsets = _viewModel.GetAllLayers().Select(l => (l.Id, l.CurrentOffsetX, l.CurrentOffsetY)).ToList();

            ParallaxCanvas.UpdateAllOffsets(offsets);
        });
    }

    /// <summary>
    /// 배경 설정 클릭
    /// Set background click
    /// </summary>
    private void OnSetBackgroundClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*",
            Title = "Select background image"
        };

        if (dialog.ShowDialog() == true)
        {
            // 기존 배경 제거
            // Remove existing background
            if (_viewModel.BackgroundLayer is not null)
            {
                ParallaxCanvas.RemoveLayer(_viewModel.BackgroundLayer.Id);
            }

            _viewModel.SetBackgroundCommand.Execute(dialog.FileName);

            // ParallaxCanvas에 배경 추가
            // Add background to ParallaxCanvas
            if (_viewModel.BackgroundLayer is not null)
            {
                ParallaxCanvas.AddLayer(_viewModel.BackgroundLayer.GetEntity());

                // 배경을 창 크기에 맞게 스케일
                // Scale background to fit window
                ScaleBackgroundToWindow();
            }

            UpdateUI();
        }
    }

    /// <summary>
    /// 배경 제거 클릭
    /// Remove background click
    /// </summary>
    private void OnRemoveBackgroundClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.BackgroundLayer is not null)
        {
            ParallaxCanvas.RemoveLayer(_viewModel.BackgroundLayer.Id);
        }

        _viewModel.RemoveBackgroundCommand.Execute(null);
        UpdateUI();
    }

    /// <summary>
    /// 레이어 추가 클릭
    /// Add layer click
    /// </summary>
    private void OnAddLayerClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*",
            Title = "Select layer image"
        };

        if (dialog.ShowDialog() == true)
        {
            _viewModel.AddLayerCommand.Execute(dialog.FileName);

            // ParallaxCanvas에 레이어 추가
            // Add layer to ParallaxCanvas
            var layer = _viewModel.Layers.LastOrDefault();
            if (layer is not null)
            {
                ParallaxCanvas.AddLayer(layer.GetEntity());
            }

            UpdateUI();
        }
    }

    /// <summary>
    /// 프리셋 변경
    /// Preset changed
    /// </summary>
    private void OnPresetChanged(ParticleType type)
    {
        _viewModel.SetPresetCommand.Execute(type);
        UpdateParticleCanvas();
    }

    /// <summary>
    /// UI 갱신
    /// Update UI
    /// </summary>
    private void UpdateUI()
    {
        // 배경 경로 표시
        // Display background path
        BackgroundPathText.Text = string.IsNullOrEmpty(_viewModel.BackgroundImagePath)
            ? "(None)"
            : Path.GetFileName(_viewModel.BackgroundImagePath);

        // 레이어 리스트 갱신 (파일명 표시)
        // Update layer list (show filename)
        LayerListBox.ItemsSource = null;
        LayerListBox.ItemsSource = _viewModel.Layers
            .OrderBy(l => l.ZIndex)
            .Select(l => $"Z:{l.ZIndex} - {Path.GetFileName(l.ImagePath)}")
            .ToList();

        // 프리셋 라디오 버튼 갱신
        // Update preset radio buttons
        PresetNoneRadio.IsChecked = _viewModel.PresetType == ParticleType.None;
        PresetSnowRadio.IsChecked = _viewModel.PresetType == ParticleType.Snow;
        PresetRainRadio.IsChecked = _viewModel.PresetType == ParticleType.Rain;

        // 프리셋 Z-Index 텍스트 갱신
        // Update preset Z-Index text
        PresetZIndexText.Text = _viewModel.PresetZIndex.ToString();
    }

    /// <summary>
    /// 파티클 캔버스 업데이트
    /// Update particle canvas
    /// </summary>
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

    #region 이미지 조작 (Image Manipulation)

    // 선택된 레이어 ID
    // Selected layer ID
    private Guid? _selectedLayerId;

    // 드래그 상태
    // Drag state
    private bool _isDraggingLayer;
    private bool _isResizingLayer;
    private Point _manipulationStartPoint;
    private Rect _originalBounds;

    // 리사이즈 방향
    // Resize direction
    private ResizeDirection _resizeDirection;

    // 선택 표시 요소들
    // Selection indicator elements
    private System.Windows.Shapes.Rectangle? _selectionRect;
    private readonly List<System.Windows.Shapes.Rectangle> _resizeHandles = [];

    // 핸들 크기
    // Handle size
    private const double HandleSize = 8;

    /// <summary>
    /// 리사이즈 방향 열거형
    /// Resize direction enum
    /// </summary>
    private enum ResizeDirection
    {
        None,
        TopLeft, Top, TopRight,
        Left, Right,
        BottomLeft, Bottom, BottomRight
    }

    /// <summary>
    /// 선택 오버레이 마우스 다운 처리
    /// Handle selection overlay mouse down
    /// </summary>
    private void OnSelectionOverlayMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_windowModeService.CurrentMode != AppMode.Edit)
        {
            return;
        }

        var pos = e.GetPosition(ParallaxCanvas);

        // 리사이즈 핸들 체크
        // Check resize handles
        _resizeDirection = GetResizeDirection(e.GetPosition(SelectionOverlay));
        if (_resizeDirection != ResizeDirection.None && _selectedLayerId.HasValue)
        {
            _isResizingLayer = true;
            _manipulationStartPoint = pos;
            _originalBounds = ParallaxCanvas.GetLayerBounds(_selectedLayerId.Value) ?? new Rect();
            SelectionOverlay.CaptureMouse();
            e.Handled = true;
            return;
        }

        // 레이어 히트 테스트
        // Layer hit test
        var hitLayerId = ParallaxCanvas.HitTestLayer(pos);

        if (hitLayerId.HasValue)
        {
            SelectLayer(hitLayerId.Value);
            _isDraggingLayer = true;
            _manipulationStartPoint = pos;
            _originalBounds = ParallaxCanvas.GetLayerBounds(hitLayerId.Value) ?? new Rect();
            SelectionOverlay.CaptureMouse();
        }
        else
        {
            ClearLayerSelection();
        }

        e.Handled = true;
    }

    /// <summary>
    /// 선택 오버레이 마우스 업 처리
    /// Handle selection overlay mouse up
    /// </summary>
    private void OnSelectionOverlayMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingLayer = false;
        _isResizingLayer = false;
        _resizeDirection = ResizeDirection.None;
        SelectionOverlay.ReleaseMouseCapture();
    }

    /// <summary>
    /// 선택 오버레이 마우스 이동 처리
    /// Handle selection overlay mouse move
    /// </summary>
    private void OnSelectionOverlayMouseMove(object sender, MouseEventArgs e)
    {
        if (_windowModeService.CurrentMode != AppMode.Edit || !_selectedLayerId.HasValue)
        {
            return;
        }

        var currentPos = e.GetPosition(ParallaxCanvas);

        // 레이어 이동
        // Move layer
        if (_isDraggingLayer)
        {
            var deltaX = currentPos.X - _manipulationStartPoint.X;
            var deltaY = currentPos.Y - _manipulationStartPoint.Y;

            var newX = _originalBounds.X + deltaX;
            var newY = _originalBounds.Y + deltaY;

            ParallaxCanvas.UpdateLayerPosition(_selectedLayerId.Value, newX, newY);
            UpdateSelectionIndicators();
        }
        // 레이어 리사이즈
        // Resize layer
        else if (_isResizingLayer)
        {
            var deltaX = currentPos.X - _manipulationStartPoint.X;
            var deltaY = currentPos.Y - _manipulationStartPoint.Y;

            var newBounds = CalculateResizedBounds(deltaX, deltaY);
            ParallaxCanvas.UpdateLayerTransform(
                _selectedLayerId.Value,
                newBounds.X, newBounds.Y,
                newBounds.Width, newBounds.Height);
            UpdateSelectionIndicators();
        }
        // 커서 업데이트
        // Update cursor
        else
        {
            var direction = GetResizeDirection(e.GetPosition(SelectionOverlay));
            SelectionOverlay.Cursor = GetResizeCursor(direction);
        }
    }

    /// <summary>
    /// 레이어 선택
    /// Select layer
    /// </summary>
    private void SelectLayer(Guid layerId)
    {
        _selectedLayerId = layerId;
        CreateSelectionIndicators();
        UpdateSelectionIndicators();
    }

    /// <summary>
    /// 레이어 선택 해제
    /// Clear layer selection
    /// </summary>
    private void ClearLayerSelection()
    {
        _selectedLayerId = null;
        _isDraggingLayer = false;
        _isResizingLayer = false;

        // 선택 표시 요소 제거
        // Remove selection indicators
        if (_selectionRect is not null)
        {
            SelectionOverlay.Children.Remove(_selectionRect);
            _selectionRect = null;
        }

        foreach (var handle in _resizeHandles)
        {
            SelectionOverlay.Children.Remove(handle);
        }
        _resizeHandles.Clear();
    }

    /// <summary>
    /// 선택 표시 요소 생성
    /// Create selection indicators
    /// </summary>
    private void CreateSelectionIndicators()
    {
        // 기존 요소 제거
        // Remove existing indicators
        ClearLayerSelection();
        if (!_selectedLayerId.HasValue)
        {
            return;
        }

        _selectedLayerId = _selectedLayerId.Value;

        // 선택 사각형 생성
        // Create selection rectangle
        _selectionRect = new System.Windows.Shapes.Rectangle
        {
            Stroke = System.Windows.Media.Brushes.Cyan,
            StrokeThickness = 2,
            StrokeDashArray = [4, 2],
            Fill = System.Windows.Media.Brushes.Transparent
        };
        SelectionOverlay.Children.Add(_selectionRect);

        // 리사이즈 핸들 생성 (8개)
        // Create resize handles (8)
        for (var i = 0; i < 8; i++)
        {
            var handle = new System.Windows.Shapes.Rectangle
            {
                Width = HandleSize,
                Height = HandleSize,
                Fill = System.Windows.Media.Brushes.White,
                Stroke = System.Windows.Media.Brushes.Cyan,
                StrokeThickness = 1
            };
            _resizeHandles.Add(handle);
            SelectionOverlay.Children.Add(handle);
        }
    }

    /// <summary>
    /// 선택 표시 요소 위치 업데이트
    /// Update selection indicators position
    /// </summary>
    private void UpdateSelectionIndicators()
    {
        if (!_selectedLayerId.HasValue || _selectionRect is null)
        {
            return;
        }

        var bounds = ParallaxCanvas.GetLayerBounds(_selectedLayerId.Value);
        if (!bounds.HasValue)
        {
            return;
        }

        var rect = bounds.Value;

        // 선택 사각형 위치
        // Selection rectangle position
        Canvas.SetLeft(_selectionRect, rect.X);
        Canvas.SetTop(_selectionRect, rect.Y);
        _selectionRect.Width = rect.Width;
        _selectionRect.Height = rect.Height;

        // 핸들 위치 (시계 방향: TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left)
        // Handle positions (clockwise: TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left)
        var halfHandle = HandleSize / 2;
        var positions = new Point[]
        {
            new(rect.X - halfHandle, rect.Y - halfHandle),                          // TopLeft
            new(rect.X + rect.Width / 2 - halfHandle, rect.Y - halfHandle),         // Top
            new(rect.X + rect.Width - halfHandle, rect.Y - halfHandle),             // TopRight
            new(rect.X + rect.Width - halfHandle, rect.Y + rect.Height / 2 - halfHandle), // Right
            new(rect.X + rect.Width - halfHandle, rect.Y + rect.Height - halfHandle), // BottomRight
            new(rect.X + rect.Width / 2 - halfHandle, rect.Y + rect.Height - halfHandle), // Bottom
            new(rect.X - halfHandle, rect.Y + rect.Height - halfHandle),            // BottomLeft
            new(rect.X - halfHandle, rect.Y + rect.Height / 2 - halfHandle)         // Left
        };

        for (var i = 0; i < _resizeHandles.Count && i < positions.Length; i++)
        {
            Canvas.SetLeft(_resizeHandles[i], positions[i].X);
            Canvas.SetTop(_resizeHandles[i], positions[i].Y);
        }
    }

    /// <summary>
    /// 마우스 위치로부터 리사이즈 방향 결정
    /// Determine resize direction from mouse position
    /// </summary>
    private ResizeDirection GetResizeDirection(Point pos)
    {
        if (_resizeHandles.Count < 8)
        {
            return ResizeDirection.None;
        }

        var directions = new[]
        {
            ResizeDirection.TopLeft, ResizeDirection.Top, ResizeDirection.TopRight,
            ResizeDirection.Right, ResizeDirection.BottomRight,
            ResizeDirection.Bottom, ResizeDirection.BottomLeft, ResizeDirection.Left
        };

        for (var i = 0; i < _resizeHandles.Count; i++)
        {
            var handle = _resizeHandles[i];
            var handleRect = new Rect(
                Canvas.GetLeft(handle),
                Canvas.GetTop(handle),
                HandleSize,
                HandleSize);

            if (handleRect.Contains(pos))
            {
                return directions[i];
            }
        }

        return ResizeDirection.None;
    }

    /// <summary>
    /// 리사이즈 방향에 따른 커서 반환
    /// Get cursor for resize direction
    /// </summary>
    private static Cursor GetResizeCursor(ResizeDirection direction)
    {
        return direction switch
        {
            ResizeDirection.TopLeft or ResizeDirection.BottomRight => Cursors.SizeNWSE,
            ResizeDirection.TopRight or ResizeDirection.BottomLeft => Cursors.SizeNESW,
            ResizeDirection.Top or ResizeDirection.Bottom => Cursors.SizeNS,
            ResizeDirection.Left or ResizeDirection.Right => Cursors.SizeWE,
            _ => Cursors.Arrow
        };
    }

    /// <summary>
    /// 리사이즈된 경계 계산
    /// Calculate resized bounds
    /// </summary>
    private Rect CalculateResizedBounds(double deltaX, double deltaY)
    {
        var x = _originalBounds.X;
        var y = _originalBounds.Y;
        var width = _originalBounds.Width;
        var height = _originalBounds.Height;

        // 최소 크기
        // Minimum size
        const double minSize = 20;

        switch (_resizeDirection)
        {
            case ResizeDirection.TopLeft:
                x += deltaX;
                y += deltaY;
                width -= deltaX;
                height -= deltaY;
                break;
            case ResizeDirection.Top:
                y += deltaY;
                height -= deltaY;
                break;
            case ResizeDirection.TopRight:
                y += deltaY;
                width += deltaX;
                height -= deltaY;
                break;
            case ResizeDirection.Right:
                width += deltaX;
                break;
            case ResizeDirection.BottomRight:
                width += deltaX;
                height += deltaY;
                break;
            case ResizeDirection.Bottom:
                height += deltaY;
                break;
            case ResizeDirection.BottomLeft:
                x += deltaX;
                width -= deltaX;
                height += deltaY;
                break;
            case ResizeDirection.Left:
                x += deltaX;
                width -= deltaX;
                break;
        }

        // 최소 크기 적용
        // Apply minimum size
        if (width < minSize)
        {
            width = minSize;
            if (_resizeDirection is ResizeDirection.TopLeft or ResizeDirection.Left or ResizeDirection.BottomLeft)
            {
                x = _originalBounds.Right - minSize;
            }
        }

        if (height < minSize)
        {
            height = minSize;
            if (_resizeDirection is ResizeDirection.TopLeft or ResizeDirection.Top or ResizeDirection.TopRight)
            {
                y = _originalBounds.Bottom - minSize;
            }
        }

        return new Rect(x, y, width, height);
    }

    #endregion
}

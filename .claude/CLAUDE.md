# Wonderland - 개발자 가이드

Samsung Wonderland 스타일의 Parallax 배경화면 위젯 (WPF, Windows 10/11).

## 빌드 및 실행

```bash
# 빌드
dotnet build src/Wonderland.slnx

# 실행
dotnet run --project src/Wonderland.WpfApp
```

## 솔루션 구조 (Clean Architecture)

```
src/
├── Wonderland.Domain/           # 엔티티, ValueObject, Enum
├── Wonderland.Application/      # 비즈니스 로직, 인터페이스
├── Wonderland.Infrastructure/   # JSON 저장/로드 구현
├── Wonderland.ViewModels/       # MVVM ViewModel (UI 독립)
├── Wonderland.WpfServices/      # WPF 서비스 (마우스 추적, 창 모드)
├── Wonderland.UI/               # CustomControl (ParallaxCanvas, ParticleCanvas)
└── Wonderland.WpfApp/           # 실행 진입점, DI 설정
    └── Services/Editing/        # 편집 모드 서비스 (Undo, Selection, Manipulation)
```

## 주요 파일

| 파일 | 역할 |
|------|------|
| `Wonderland.UI/Controls/ParallaxCanvas.cs` | DrawingVisual 기반 레이어 렌더링 |
| `Wonderland.UI/Controls/ParticleCanvas.cs` | CompositionTarget.Rendering 파티클 (DependencyProperty, Brush 캐싱) |
| `Wonderland.WpfServices/WindowModeService.cs` | WS_EX_TRANSPARENT Viewer/Edit 전환 |
| `Wonderland.WpfServices/MouseTrackingService.cs` | 마우스 위치 추적 및 정규화 |
| `Wonderland.WpfApp/Services/Editing/UndoService.cs` | Undo 스택 관리 |
| `Wonderland.WpfApp/Services/Editing/LayerManipulationService.cs` | 레이어 조작 로직 |
| `Wonderland.WpfApp/Services/Editing/SelectionIndicatorService.cs` | 선택 UI 요소 관리 |
| `Wonderland.WpfApp/Themes/EditorStyles.xaml` | 에디터 스타일 허브 (MergedDictionaries) |

## 아키텍처 노트

### DataTemplate 매핑 미사용 이유
- 단일 MainWindow 구조로 View 전환 없음
- MainWindow.xaml.cs가 WPF 서비스 조정 역할 담당 (WindowModeService, MouseTrackingService, TrayIconService 등)
- 이러한 서비스들은 WPF 의존성이 있어 ViewModel로 이동 불가 (MVVM 위반 방지)
- DataContext 직접 주입이 현재 구조에 적합
- `Resources/Mappings.xaml`은 향후 확장(다중 View 전환)을 위해 준비됨

### 편집 서비스 분리
- `Services/Editing/` 폴더에 편집 관련 서비스 집중
- 각 서비스는 단일 책임 원칙 준수
- MainWindow는 서비스 조합 및 UI 이벤트 핸들링만 담당

### ParticleCanvas 성능 최적화
- DependencyProperty 사용 (ParticleType, Settings, IsActive)
- Opacity별 Brush 캐싱 (0.1 단위, 11개) - 매 프레임 Clone() 제거로 GC 부하 감소
- Freeze()된 브러시 재사용

### EditorStyles 구조
```
Themes/
├── EditorStyles.xaml           # 허브 (MergedDictionaries)
└── Editor/
    ├── EditorColors.xaml       # 색상 리소스 (가장 먼저 로드)
    ├── EditorTextStyles.xaml   # 텍스트/패널 스타일
    ├── EditorButtonStyles.xaml # 버튼 스타일
    ├── EditorListBoxStyles.xaml# ListBox 스타일
    └── EditorMiscStyles.xaml   # RadioButton, Separator 등
```
- 분리된 파일에서 색상 참조 시 `DynamicResource` 사용 (로드 순서 독립)
- `BasedOn` 스타일 참조는 `StaticResource` 유지 (WPF 제약)

## 단축키

| 키 | 동작 |
|----|------|
| F12 | Viewer/Edit 모드 토글 |
| ESC | Edit 모드 종료 (Viewer로 전환) |
| Ctrl+Z | Undo (Edit 모드에서만) |
| Delete | 선택된 레이어 삭제 (Edit 모드에서만) |

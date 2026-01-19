# Wonderland - WPF Parallax Widget

Samsung Wonderland 스타일의 Parallax 배경화면 위젯을 WPF로 구현한 Windows 10/11용 데스크톱 앱.

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
```

## 핵심 기능

| 기능 | 상태 | 설명 |
|------|------|------|
| Parallax 효과 | ✅ 완료 | 마우스 이동에 따라 레이어가 다른 속도로 움직여 깊이감 생성 |
| 다중 모니터 지원 | ✅ 완료 | VirtualScreen 좌표 사용으로 다중 모니터 환경 지원 |
| 투명 창 | ✅ 완료 | WindowStyle=None, AllowsTransparency, WS_EX_TRANSPARENT |
| Viewer/Edit 모드 | ✅ 완료 | F12 토글, ESC로 Edit 모드 종료 |
| 파티클 시스템 | ✅ 완료 | Snow, Rain 프리셋 |
| JSON 설정 저장 | ✅ 완료 | appsettings.json에 창 위치, 레이어, 파티클 설정 저장 |
| 시스템 트레이 | ✅ 완료 | 트레이 아이콘, 우클릭 메뉴, 더블클릭 Edit 모드 |
| 단일 인스턴스 | ✅ 완료 | Mutex 기반 중복 실행 방지 |

## 알려진 이슈 (TODO)

### 1. Edit 모드 이미지 선택 불가 (우선순위: 높음)
- **증상**: Edit 모드에서 이미지 클릭 시 선택되지 않음
- **원인 추정**: SelectionOverlay Canvas가 마우스 이벤트를 받지 못함
- **관련 파일**:
  - `src/Wonderland.WpfApp/MainWindow.xaml.cs` - SelectionOverlay 이벤트 핸들링
  - `src/Wonderland.WpfApp/MainWindow.xaml` - SelectionOverlay Canvas 정의

### 2. 원격 저장소 Push 대기
- **상태**: 원격 저장소 생성 대기 중
- **작업**: https://github.com/christian289/Wonderland 저장소 생성 후 push 필요
- **명령어**: `git push -u origin main`

## 최근 변경 이력

### 2026-01-17
- **fix**: TrayIcon 잔상 및 Mutex 정리 문제 수정
  - `NotifyIcon.Visible = false` 설정 후 Dispose
  - Icon 핸들 메모리 누수 수정 (DestroyIcon P/Invoke)
  - `Environment.Exit(0)` → `Application.Shutdown()` 변경

- **fix**: Parallax X축 이동 문제 수정
  - 다중 모니터 환경에서 X좌표가 항상 1.0으로 고정되던 문제
  - `PrimaryScreen` → `VirtualScreen` 좌표계 변경

- **feat**: 초기 구현 완료
  - Clean Architecture 구조
  - DrawingVisual 기반 고성능 렌더링
  - Global Mouse/Keyboard Hook

## 기술 스택

- .NET 9.0
- WPF (Windows Presentation Foundation)
- CommunityToolkit.Mvvm
- Microsoft.Extensions.Hosting (GenericHost DI)

## 주요 파일

| 파일 | 역할 |
|------|------|
| `Wonderland.UI/Controls/ParallaxCanvas.cs` | DrawingVisual 기반 레이어 렌더링 |
| `Wonderland.UI/Controls/ParticleCanvas.cs` | CompositionTarget.Rendering 파티클 |
| `Wonderland.WpfServices/WindowModeService.cs` | WS_EX_TRANSPARENT Viewer/Edit 전환 |
| `Wonderland.WpfServices/MouseTrackingService.cs` | 마우스 위치 추적 및 정규화 |
| `Wonderland.Application/Services/ParallaxCalculator.cs` | 레이어별 오프셋 계산 |

## 단축키

| 키 | 동작 |
|----|------|
| F12 | Viewer/Edit 모드 토글 |
| ESC | Edit 모드 종료 (Viewer로 전환) |

## 아키텍처 노트

### DataTemplate 매핑 미사용 이유
- 단일 MainWindow 구조로 View 전환 없음
- MainWindow.xaml.cs가 WPF 서비스 조정 역할 담당 (WindowModeService, MouseTrackingService, TrayIconService 등)
- 이러한 서비스들은 WPF 의존성이 있어 ViewModel로 이동 불가 (MVVM 위반 방지)
- DataContext 직접 주입이 현재 구조에 적합
- `Resources/Mappings.xaml`은 향후 확장(다중 View 전환)을 위해 준비됨

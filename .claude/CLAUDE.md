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
    └── Services/Editing/        # 편집 모드 서비스 (Undo, Selection, Manipulation)
```

## 도메인 용어 정의

팀 내 커뮤니케이션을 위한 용어 정의입니다.

### 레이어 관련

| 용어 | 설명 |
|------|------|
| **Layer** | 이미지 레이어. 배경(Background) 또는 전경(Foreground)으로 구분 |
| **Background Layer** | Z-Index가 0인 레이어. 창 크기에 맞게 자동 스케일 |
| **Foreground Layer** | Z-Index가 1 이상인 레이어. 사용자가 위치/크기/회전 조작 가능 |
| **Z-Index** | 레이어의 렌더링 순서. 높을수록 앞에 표시 |
| **Depth Factor** | Parallax 효과의 강도 계수. 0.0~1.0 범위 |

### Parallax 효과

| 용어 | 설명 |
|------|------|
| **Parallax** | 마우스 위치에 따라 레이어가 다른 속도로 이동하여 깊이감을 만드는 효과 |
| **Offset** | Parallax 효과로 인한 레이어의 현재 이동량 (OffsetX, OffsetY) |
| **Max Offset** | 레이어가 이동할 수 있는 최대 픽셀 값 |

### 앱 모드

| 용어 | 설명 |
|------|------|
| **Viewer Mode** | 기본 모드. 마우스 클릭이 창을 통과 (WS_EX_TRANSPARENT). Parallax 효과 활성화 |
| **Edit Mode** | 편집 모드. 이미지 선택/이동/크기조절/회전 가능. 에디터 패널 표시 |

### 선택 및 조작

| 용어 | 설명 |
|------|------|
| **Selection Indicator** | 선택된 레이어의 UI 표시. 점선 사각형 + 리사이즈 핸들 + 회전 핸들 |
| **Resize Handle** | 크기 조절용 핸들. 8개 방향 (모서리 4개 + 변 중앙 4개) |
| **Rotation Handle** | 회전용 핸들. 선택 사각형 상단 중앙에 위치한 녹색 원 |
| **Resize Direction** | 리사이즈 방향 (TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left) |

### Undo 시스템

| 용어 | 설명 |
|------|------|
| **Edit Action** | 실행 취소 가능한 편집 동작. IEditAction 인터페이스 구현 |
| **Move Action** | 레이어 이동 동작 |
| **Resize Action** | 레이어 크기 조절 동작 |
| **Rotate Action** | 레이어 회전 동작 |
| **Undo Stack** | 최대 50개까지 저장되는 동작 스택 |

### 파티클 시스템

| 용어 | 설명 |
|------|------|
| **Particle** | 눈, 비 등의 개별 입자 |
| **Particle Preset** | 미리 정의된 파티클 설정 (None, Snow, Rain) |
| **Spawn Rate** | 초당 생성되는 파티클 수 |

## 핵심 기능

| 기능 | 상태 | 설명 |
|------|------|------|
| Parallax 효과 | ✅ 완료 | 마우스 이동에 따라 레이어가 다른 속도로 움직여 깊이감 생성 |
| 다중 모니터 지원 | ✅ 완료 | VirtualScreen 좌표 사용으로 다중 모니터 환경 지원 |
| 투명 창 | ✅ 완료 | WindowStyle=None, AllowsTransparency, WS_EX_TRANSPARENT |
| Viewer/Edit 모드 | ✅ 완료 | F12 토글, ESC로 Edit 모드 종료 |
| 이미지 선택/이동 | ✅ 완료 | Edit 모드에서 레이어 클릭으로 선택, 드래그로 이동 |
| 이미지 크기 조절 | ✅ 완료 | 8방향 핸들로 리사이즈. 대각선은 종횡비 유지 |
| 이미지 회전 | ✅ 완료 | 회전 핸들로 자유 회전. 선택 UI도 함께 회전 |
| Ctrl+Z Undo | ✅ 완료 | 이동/크기조절/회전 동작 실행 취소 (최대 50개) |
| Delete 키 삭제 | ✅ 완료 | 선택된 레이어 삭제 |
| 자동 저장 | ✅ 완료 | Edit 모드 종료 시 appsettings.json에 자동 저장 |
| 파티클 시스템 | ✅ 완료 | Snow, Rain 프리셋 |
| JSON 설정 저장 | ✅ 완료 | appsettings.json에 창 위치, 레이어, 파티클 설정 저장 |
| 시스템 트레이 | ✅ 완료 | 트레이 아이콘, 우클릭 메뉴, 더블클릭 Edit 모드 |
| 단일 인스턴스 | ✅ 완료 | Mutex 기반 중복 실행 방지 |

## 알려진 이슈 (TODO)

### 1. 원격 저장소 Push 대기
- **상태**: 원격 저장소 생성 대기 중
- **작업**: https://github.com/christian289/Wonderland 저장소 생성 후 push 필요
- **명령어**: `git push -u origin main`

## 최근 변경 이력

### 2026-01-19
- **refactor**: 코드 가독성 개선을 위한 서비스 분리
  - `UndoService`: Undo 스택 관리
  - `LayerManipulationService`: 레이어 선택/드래그/리사이즈/회전 로직
  - `SelectionIndicatorService`: 선택 UI 요소 관리
  - MainWindow.xaml.cs: 1578줄 → 858줄 (약 45% 감소)

### 2026-01-18
- **feat**: Edit 모드 기능 대폭 개선
  - Ctrl+Z Undo 기능 (이동/크기조절/회전)
  - Delete 키로 레이어 삭제
  - 이미지 회전 시 선택 틀 동기화
  - 대각선 리사이즈 시 종횡비 유지
  - 배경 레이어 선택 불가
  - 레이어 변환 정보 영속성 (X, Y, Width, Height, Rotation)
  - Edit 모드 종료 시 자동 저장

### 2026-01-17
- **fix**: TrayIcon 잔상 및 Mutex 정리 문제 수정
- **fix**: Parallax X축 이동 문제 수정 (다중 모니터)
- **feat**: 초기 구현 완료

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
| `Wonderland.WpfApp/Services/Editing/UndoService.cs` | Undo 스택 관리 |
| `Wonderland.WpfApp/Services/Editing/LayerManipulationService.cs` | 레이어 조작 로직 |
| `Wonderland.WpfApp/Services/Editing/SelectionIndicatorService.cs` | 선택 UI 요소 관리 |

## 단축키

| 키 | 동작 |
|----|------|
| F12 | Viewer/Edit 모드 토글 |
| ESC | Edit 모드 종료 (Viewer로 전환) |
| Ctrl+Z | Undo (Edit 모드에서만) |
| Delete | 선택된 레이어 삭제 (Edit 모드에서만) |

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

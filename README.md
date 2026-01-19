# Wonderland

Samsung Wonderland 스타일의 Parallax 배경화면 위젯을 WPF로 구현한 Windows 10/11용 데스크톱 앱입니다.

![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4)
![WPF](https://img.shields.io/badge/WPF-Windows-0078D4)
![License](https://img.shields.io/badge/License-MIT-green)

## 주요 기능

- **Parallax 효과**: 마우스 이동에 따라 레이어가 다른 속도로 움직여 깊이감 생성
- **다중 레이어 지원**: 배경(Z-Index 0) + 전경 레이어(Z-Index 1~10)
- **파티클 시스템**: Snow, Rain 프리셋 제공
- **Edit 모드**: 이미지 선택, 이동, 크기 조절, 회전 지원
- **Undo 지원**: Ctrl+Z로 편집 작업 실행 취소 (최대 50개)
- **자동 저장**: Edit 모드 종료 시 설정 자동 저장
- **투명 창**: 마우스 클릭이 창을 통과 (Viewer 모드)
- **다중 모니터 지원**: VirtualScreen 좌표 사용
- **시스템 트레이**: 트레이 아이콘으로 백그라운드 실행

## 스크린샷

<!-- TODO: 스크린샷 추가 -->

## 설치 및 실행

### 요구 사항

- Windows 10/11
- [.NET 9.0 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)

### 빌드

```bash
# 저장소 클론
git clone https://github.com/christian289/Wonderland.git
cd Wonderland

# 빌드
dotnet build src/Wonderland.slnx

# 실행
dotnet run --project src/Wonderland.WpfApp
```

### 릴리스 빌드

```bash
dotnet publish src/Wonderland.WpfApp -c Release -o ./publish
```

## 사용 방법

### 단축키

| 키 | 동작 |
|----|------|
| **F12** | Viewer/Edit 모드 토글 |
| **ESC** | Edit 모드 종료 (Viewer로 전환) |
| **Ctrl+Z** | Undo (Edit 모드에서만) |
| **Delete** | 선택된 레이어 삭제 (Edit 모드에서만) |

### 모드 설명

#### Viewer 모드 (기본)
- 마우스 클릭이 창을 통과 (WS_EX_TRANSPARENT)
- Parallax 효과 활성화
- 시스템 트레이에서 실행

#### Edit 모드
- F12 또는 트레이 아이콘 더블클릭으로 진입
- 에디터 패널이 우측에 표시됨
- 이미지 선택, 이동, 크기 조절, 회전 가능
- 배경/레이어/파티클 설정 변경

### 레이어 조작 (Edit 모드)

- **선택**: 이미지 클릭
- **이동**: 선택 후 드래그
- **크기 조절**: 모서리/변 핸들 드래그 (대각선은 종횡비 유지)
- **회전**: 상단 녹색 핸들 드래그

## 설정 파일

설정은 `appsettings.json`에 저장됩니다:

```json
{
  "Window": {
    "Left": 100,
    "Top": 100,
    "Width": 300,
    "Height": 600
  },
  "Layers": [
    {
      "Id": "...",
      "ImagePath": "path/to/image.png",
      "ZIndex": 0,
      "DepthFactor": 0.0,
      "X": 0,
      "Y": 0,
      "Width": 300,
      "Height": 600,
      "Rotation": 0
    }
  ],
  "ParticlePreset": "None"
}
```

## 프로젝트 구조

```
src/
├── Wonderland.Domain/           # 엔티티, ValueObject, Enum
├── Wonderland.Application/      # 비즈니스 로직, 인터페이스
├── Wonderland.Infrastructure/   # JSON 저장/로드 구현
├── Wonderland.ViewModels/       # MVVM ViewModel (UI 독립)
├── Wonderland.WpfServices/      # WPF 서비스 (마우스 추적, 창 모드)
├── Wonderland.UI/               # CustomControl (ParallaxCanvas, ParticleCanvas)
└── Wonderland.WpfApp/           # 실행 진입점, DI 설정
    └── Services/Editing/        # 편집 모드 서비스
```

## 용어 정의

### 레이어

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

### 선택 및 조작

| 용어 | 설명 |
|------|------|
| **Selection Indicator** | 선택된 레이어의 UI 표시. 점선 사각형 + 리사이즈 핸들 + 회전 핸들 |
| **Resize Handle** | 크기 조절용 핸들. 8개 방향 (모서리 4개 + 변 중앙 4개) |
| **Rotation Handle** | 회전용 핸들. 선택 사각형 상단 중앙에 위치한 녹색 원 |

### 파티클 시스템

| 용어 | 설명 |
|------|------|
| **Particle** | 눈, 비 등의 개별 입자 |
| **Particle Preset** | 미리 정의된 파티클 설정 (None, Snow, Rain) |

## 기술 스택

- [.NET 9.0](https://dotnet.microsoft.com/)
- [WPF](https://docs.microsoft.com/ko-kr/dotnet/desktop/wpf/) (Windows Presentation Foundation)
- [CommunityToolkit.Mvvm](https://docs.microsoft.com/ko-kr/dotnet/communitytoolkit/mvvm/)
- [Microsoft.Extensions.Hosting](https://docs.microsoft.com/ko-kr/dotnet/core/extensions/generic-host) (GenericHost DI)

## 라이선스

MIT License - 자유롭게 사용, 수정, 배포할 수 있습니다.

## 기여

버그 리포트, 기능 제안, PR을 환영합니다.

1. Fork
2. Feature branch 생성 (`git checkout -b feature/amazing-feature`)
3. Commit (`git commit -m 'Add amazing feature'`)
4. Push (`git push origin feature/amazing-feature`)
5. Pull Request 생성

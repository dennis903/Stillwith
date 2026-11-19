# 개발 환경 세팅

> 작성일: 2026-11-18 · 기준: 기획서 v0.3, ADR 0001·0002

## 1. 설치 (Mac, 한 번만)

1. **Unity Hub** 설치 → Installs → **Unity 6** 설치. 현재 프로젝트는 **6.6 (6000.6.9f1)**로 시작했다. 6.7 LTS가 나오면 올린다. 모듈:
   - iOS Build Support
   - Android Build Support (OpenJDK, Android SDK & NDK Tools 함께 체크)
2. **Xcode** (App Store). iOS 시뮬레이터와 iOS 빌드에 필요하다.
3. **Git LFS**
   ```sh
   brew install git-lfs
   git lfs install
   ```
4. **코드 편집기**: VS Code + "Unity" 확장(C# Dev Kit 포함), 또는 Rider. Unity에서 Settings → External Tools → External Script Editor로 지정.

## 2. Unity 프로젝트 만들기

1. Unity Hub → New project → 템플릿 **Universal 3D** (URP).
2. Project name: `App`, Location: 이 레포 폴더(`~/Stillwith`) → `~/Stillwith/App`이 생긴다.
   - 폴더 이름 `App`은 앱 이름이 정해지기 전까지 쓰는 중립 이름이다. 스토어에 보이는 이름은 Product Name으로 따로 정한다.
3. 템플릿 예제가 있으면 지운다: `Assets/TutorialInfo/`, `Assets/Readme.asset`.

## 3. 처음 할 프로젝트 설정

Edit → Project Settings.

| 위치 | 설정 | 이유 |
|---|---|---|
| Editor → Asset Serialization | Force Text | git에서 차이가 보이게 (Unity 6 기본값, 확인만) |
| Version Control → Mode | Visible Meta Files | `.meta`를 git에 넣기 위해 (기본값, 확인만) |
| Player → Company / Product Name | 임시 값 | 앱 이름 확정 전 |
| Player → Resolution and Presentation → Default Orientation | Auto Rotation, 네 방향 모두 허용 | 가로·세로 지원 (ADR 0001) |
| Player → Other Settings → Color Space | Linear | URP 템플릿 기본값, 확인만 |
| Player → iOS → Other Settings → Camera Usage Description | "자리에 있는지만 확인하고, 영상은 저장하거나 보내지 않아요." | 비어 있으면 카메라 권한 요청이 막힌다 (기획서 8.13 가정 문구) |
| Player → iOS → Other Settings → Target SDK | 시뮬레이터: Simulator SDK / 실기기: Device SDK | 시뮬레이터용 빌드는 Simulator SDK로 따로 만들어야 한다 |
| Quality / Graphics | 템플릿의 모바일용 URP 에셋을 iOS·Android 기본으로 | 성능 목표 30fps (기획서 10장) |

## 4. 시뮬레이터로 확인할 수 있는 것과 없는 것

| 확인 | 시뮬레이터 | 대신 쓸 곳 |
|---|---|---|
| 화면비별 화각, 세 겹 영역, 가로·세로 전환 | 가능 (기기 종류를 바꿔 가며) | — |
| UI, 상태 전환, 세션 계산 | 가능 | — |
| 카메라 감지 | 불가 (iOS 시뮬레이터에는 카메라가 없다) | Unity 에디터 Play 모드 + Mac 웹캠 |
| 30fps, 발열, 배터리 | 의미 없음 (Mac의 성능으로 돈다) | 실기기 (M1 측정) |

## 5. 레포 구조

```
Stillwith/
├─ GLOSSARY.md
├─ docs/                 기획서, ADR, 이 문서
├─ art/                  Unity가 직접 읽지 않는 원본 (.blend)
│  └─ blender/
└─ App/                  Unity 프로젝트
   ├─ Packages/
   ├─ ProjectSettings/
   └─ Assets/
      ├─ _Project/       직접 만드는 것 전부
      │  ├─ Core/        공통 로직 (기획서 7장)
      │  ├─ Places/      장소 묶음. 장소 하나 = 폴더 하나 (예: LakeDeck/)
      │  ├─ Characters/  캐릭터 묶음. 공통 사람형 뼈대는 _SharedRig/
      │  └─ Scenes/      Boot 장면 등 공통 장면
      ├─ Settings/       URP 설정 (템플릿이 만든 것 그대로)
      └─ ThirdParty/     MediaPipe 등 외부 플러그인
```

규칙:

- **Core는 Places와 Characters를 모른다.** Core 코드가 특정 장소나 캐릭터의 이름·에셋을 직접 참조하지 않는다. 어셈블리 정의(asmdef)로 참조 방향을 강제한다. 이래야 "공통 로직을 고치지 않고 장소·캐릭터를 추가한다"(기획서 12장 구조 기준)를 지킬 수 있다.
- **장소 폴더는 장소 규격(기획서 7.1)을 그대로 따른다.** Models, Textures, Materials, Shaders, Animations, Audio, Lighting, Prefabs.
- **`.blend` 원본은 `art/`에만 둔다.** Unity에는 FBX로 내보내 넣는다.
- **외부 플러그인은 `ThirdParty/`에.** 직접 만든 것과 섞지 않는다.

## 6. 첫 커밋 전에 확인

```sh
git lfs ls-files   # png, fbx 등이 LFS로 잡히는지
git status         # Library/, Temp/, *.csproj 가 안 보이는지
```

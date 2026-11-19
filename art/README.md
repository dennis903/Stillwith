# art

Unity가 직접 읽지 않는 원본 작업 파일을 둔다.

- `blender/` — 장소와 캐릭터의 `.blend` 원본. 장소마다 하위 폴더 하나 (예: `blender/lake-deck/`).
- Unity에는 FBX로 내보내서 넣는다. 장소 모델은 `App/Assets/_Project/Places/<장소>/Models/`로 간다.
- `.blend`를 Unity `Assets/`에 직접 넣지 않는다. 그러면 Unity가 가져올 때마다 Blender가 설치되어 있어야 하고, 원본과 결과물이 섞인다.

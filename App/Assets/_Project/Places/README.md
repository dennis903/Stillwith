# Places — 장소

장소 하나 = 폴더 하나. 장소 규격(기획서 7.1)을 채운 콘텐츠 묶음이다.

장소 폴더 안의 구성:

```
<장소>/
├─ Models/       FBX (원본 .blend는 레포 루트의 art/blender/)
├─ Textures/
├─ Materials/
├─ Shaders/      물, 불, 풀처럼 움직이는 것
├─ Animations/   이 장소의 집중·쉬기·걷기 동작 (공통 사람형 뼈대 기준)
├─ Audio/        소리 레이어 최대 4개
├─ Lighting/     구운 라이트맵 (시간대별)
└─ Prefabs/
```

폴더는 필요해질 때 만든다. 빈 폴더는 git에 올라가지 않아서 다른 기기에서 받으면 Unity가 경고를 띄운다.

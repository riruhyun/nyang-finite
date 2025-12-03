# ScreenFadeController 설정 가이드

플레이어 사망 시 페이드 효과와 씬 재시작이 자동으로 작동하도록 설정하는 방법입니다.

## 1. Canvas 생성

1. Unity 에디터에서 Hierarchy 창 우클릭
2. `UI > Canvas` 선택하여 Canvas 생성
3. Canvas 이름을 "FadeCanvas"로 변경

## 2. Canvas 설정

Canvas의 Inspector에서:
- **Render Mode**: `Screen Space - Overlay` (가장 위에 표시)
- **Sort Order**: `999` (모든 UI 위에 표시)

## 3. Fade Image 생성

1. FadeCanvas 우클릭
2. `UI > Image` 선택
3. 생성된 Image 이름을 "FadeImage"로 변경

## 4. FadeImage 설정

FadeImage의 Inspector에서:
- **Rect Transform**을 전체 화면으로:
  - Anchor: Stretch-Stretch (왼쪽 위 버튼 누른 상태로 Alt+Shift+클릭)
  - Left, Right, Top, Bottom 모두 `0`
- **Image > Color**: 검은색 (R:0, G:0, B:0, A:255)
- **Image > Raycast Target**: ✓ 체크

## 5. CanvasGroup 추가

1. FadeImage 선택
2. Inspector 하단의 `Add Component` 버튼
3. "Canvas Group" 검색 후 추가
4. **CanvasGroup > Alpha**: `0` (시작 시 투명)
5. **CanvasGroup > Blocks Raycasts**: ✓ 체크

## 6. ScreenFadeController 추가

1. FadeCanvas 선택
2. Inspector 하단의 `Add Component` 버튼
3. "ScreenFadeController" 검색 후 추가

## 7. ScreenFadeController 설정

ScreenFadeController 컴포넌트에서:
- **Fade Canvas Group**: FadeImage의 CanvasGroup을 드래그 앤 드롭
- **Fade Duration**: `1` (페이드 지속 시간 1초)
- **Fade On Scene Start**: ✓ 체크 (씬 시작 시 페이드 인 효과)

## 8. 완료!

이제 플레이어가 죽으면:
1. 3초 대기
2. 화면이 검게 페이드 아웃
3. 현재 씬이 재시작
4. 화면이 밝아지는 페이드 인

모든 스테이지에서 작동하도록 FadeCanvas는 `DontDestroyOnLoad`로 설정되어 있습니다.

## 참고: 대기 시간 변경

GameManager.cs 120번 줄에서 대기 시간을 조정할 수 있습니다:
```csharp
ScreenFadeController.Instance.RespawnWithFade(3f); // 3초 대기
```

원하는 시간(초)으로 변경 가능합니다.
